using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing.Facets
{
    public class FaultTolerantWorkflowIndexWriter<TGrainState> : NonFaultTolerantWorkflowIndexWriter<TGrainState>,
                                                                 IFaultTolerantWorkflowIndexWriter<TGrainState> where TGrainState : class, new()
    {
        private readonly IGrainFactory _grainFactory;    // TODO: standardize leading _ or not; and don't do this._

        public FaultTolerantWorkflowIndexWriter(
                IServiceProvider sp,
                IIndexWriterConfiguration config,
                IGrainFactory grainFactory
            ) : base(sp, config)
        {
            this._grainFactory = grainFactory;
        }

        private bool _hasAnyTotalIndex;

        private FaultTolerantIndexableGrainStateWrapper<TGrainState> ftWrappedState;

        internal override IDictionary<Type, IIndexWorkflowQueue> WorkflowQueues
        {
            get => this.ftWrappedState.WorkflowQueues;
            set => this.ftWrappedState.WorkflowQueues = value;
        }

        private HashSet<Guid> ActiveWorkflowsSet
        {
            get => this.ftWrappedState.ActiveWorkflowsSet;
            set => this.ftWrappedState.ActiveWorkflowsSet = value;
        }

        public async override Task OnActivateAsync(Grain grain, IndexableGrainStateWrapper<TGrainState> wrappedState,
                                                   Func<Task> writeGrainStateFunc, Func<Task> onGrainActivateFunc)
        {
            this.ftWrappedState = wrappedState as FaultTolerantIndexableGrainStateWrapper<TGrainState>;
            if (this.ftWrappedState == null)
            {
                throw new IndexException($"Grain type {grain.GetType().Name} state must be wrapped by FaultTolerantIndexableGrainStateWrapper");
            }

            base.InitOnActivate(grain, wrappedState, writeGrainStateFunc);

            // If the list of active workflows is null or empty we can assume that we were not previously activated
            // or did not have any incomplete workflow queue items in a prior activation.
            if (this.ActiveWorkflowsSet == null || this.ActiveWorkflowsSet.Count == 0)
            {
                this.WorkflowQueues = null;
                await base.OnActivateAsync(grain, wrappedState, onGrainActivateFunc, writeGrainStateFunc);
            }
            else
            {
                // There are some remaining active workflows so they should be handled first.
                this.PruneWorkflowQueuesForMissingInterfaceTypes();
                await this.HandleRemainingWorkflows()
                           .ContinueWith(t => Task.WhenAll(this.PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(t.Result),
                                                           base.OnActivateAsync(grain, wrappedState, onGrainActivateFunc, writeGrainStateFunc)));
            }
            this._hasAnyTotalIndex = this._grainIndexes.HasAnyTotalIndex;
        }

        /// <summary>
        /// Applies a set of updates to the indexes defined on the grain
        /// </summary>
        /// <param name="updatesByInterface">the dictionary of indexes to their corresponding updates</param>
        /// <param name="updateIndexesEagerly">whether indexes should be updated eagerly or lazily</param>
        /// <param name="onlyUniqueIndexesWereUpdated">a flag to determine whether only unique indexes were updated</param>
        /// <param name="numberOfUniqueIndexesUpdated">determine the number of updated unique indexes</param>
        /// <param name="writeStateIfConstraintsAreNotViolated">whether writing back
        ///             the state to the storage should be done if no constraint is violated</param>
        private protected override async Task ApplyIndexUpdates(InterfaceToUpdatesMap updatesByInterface,
                                                                bool updateIndexesEagerly, bool onlyUniqueIndexesWereUpdated,
                                                                int numberOfUniqueIndexesUpdated, bool writeStateIfConstraintsAreNotViolated)
        {
            if (updatesByInterface.IsEmpty || !this._hasAnyTotalIndex)
            {
                await base.ApplyIndexUpdates(updatesByInterface, updateIndexesEagerly, onlyUniqueIndexesWereUpdated,
                                             numberOfUniqueIndexesUpdated, writeStateIfConstraintsAreNotViolated);
                return;
            }

            if (updateIndexesEagerly)
            {
                throw new InvalidOperationException("Fault tolerant indexes cannot be updated eagerly. This misconfiguration should have" +
                                                    " been detected on silo startup. Check ApplicationPartsIndexableGrainLoader for the reason.");
            }

            var workflowId = this.GenerateUniqueWorkflowId();

            // Update the indexes lazily. This is the first step, because its workflow record should be persisted in the workflow-queue first.
            // The reason for waiting here is to make sure that the workflow record in the workflow queue is correctly persisted.
            await base.ApplyIndexUpdatesLazily(updatesByInterface, workflowId);

            // Apply any unique index updates eagerly.
            if (numberOfUniqueIndexesUpdated > 0)
            {
                // If there is more than one unique index to update, then updates to the unique indexes should be tentative
                // so they are not visible to readers before making sure that all uniqueness constraints are satisfied.
                // UniquenessConstraintViolatedExceptions propagate; any tentative records will be removed by WorkflowQueueHandler.
                await base.ApplyIndexUpdatesEagerly(updatesByInterface, UpdateIndexType.Unique, updateIndexesTentatively: true);
            }

            // Finally, the grain state is persisted if requested.
            if (writeStateIfConstraintsAreNotViolated)
            {
                // There is no constraint violation and the workflow ID can be a part of the list of active workflows.
                // Here, we add the work-flow to the list of committed/in-flight work-flows
                this.AddWorkflowIdToActiveWorkflows(workflowId);
                await base.writeGrainStateFunc();
            }

            // If everything was successful, the before images are updated
            base.UpdateBeforeImages(updatesByInterface);
        }

        /// <summary>
        /// Handles the remaining work-flows of the grain 
        /// </summary>
        /// <returns>the actual list of work-flow record IDs that were available in the queue(s)</returns>
        private Task<IEnumerable<Guid>> HandleRemainingWorkflows()
        {
            // A copy of WorkflowQueues is required, because we want to iterate over it and add/remove elements from/to it.
            var copyOfWorkflowQueues = new Dictionary<Type, IIndexWorkflowQueue>(this.WorkflowQueues);
            var tasks = copyOfWorkflowQueues.Select(wfqEntry => this.HandleRemainingWorkflows(wfqEntry.Key, wfqEntry.Value));
            return Task.WhenAll(tasks).ContinueWith(t => t.Result.SelectMany(res => res));
        }

        /// <summary>
        /// Handles the remaining work-flows of a specific grain interface of the grain
        /// </summary>
        /// <param name="iGrainType">the grain interface type being indexed</param>
        /// <param name="oldWorkflowQ">the previous work-flow queue responsible for handling the updates</param>
        /// <returns>the actual list of work-flow record IDs that were available in this queue</returns>
        private async Task<IEnumerable<Guid>> HandleRemainingWorkflows(Type iGrainType, IIndexWorkflowQueue oldWorkflowQ)
        {
            // Keeps the reference to the reincarnated work-flow queue, if the original work-flow queue (GrainService) did not respond.
            IIndexWorkflowQueue reincarnatedOldWorkflowQ = null;

            // Keeps the list of work-flow records from the old work-flow queue.
            Immutable<List<IndexWorkflowRecord>> remainingWorkflows;

            // First, we remove the work-flow queue associated with iGrainType (i.e., oldWorkflowQ) so that another call to get the
            // workflow queue for iGrainType gets the new work-flow queue responsible for iGrainType (otherwise oldWorkflowQ is returned).
            this.WorkflowQueues.Remove(iGrainType);
            var newWorkflowQ = this.GetWorkflowQueue(iGrainType);

            // If the same work-flow queue is responsible we just check what work-flow records are still in process
            if (newWorkflowQ.Equals(oldWorkflowQ))
            {
                remainingWorkflows = await oldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet);
                if (remainingWorkflows.Value != null && remainingWorkflows.Value.Count > 0)
                {
                    return remainingWorkflows.Value.Select(w => w.WorkflowId);
                }
            }
            else //the work-flow queue responsible for iGrainType has changed
            {
                try
                {
                    // We try to contact the original oldWorkflowQ to get the list of remaining work-flow records
                    // in order to pass their responsibility to newWorkflowQ.
                    remainingWorkflows = await oldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet);
                }
                catch //the corresponding workflowQ is down, we should ask its reincarnated version
                {
                    // If anything bad happened, it means that oldWorkflowQ is not reachable.
                    // Then we get our hands to reincarnatedOldWorkflowQ to get the list of remaining work-flow records.
                    reincarnatedOldWorkflowQ = await this.GetReincarnatedWorkflowQueue(oldWorkflowQ);
                    remainingWorkflows = await reincarnatedOldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet);
                }

                // If any work-flow is remaining unprocessed...
                if (remainingWorkflows.Value != null && remainingWorkflows.Value.Count > 0)
                {
                    // Give the responsibility of handling the remaining workflow records to the newWorkflowQ.
                    await newWorkflowQ.AddAllToQueue(remainingWorkflows);

                    // Check which was the target old work-flow queue that responded to our request.
                    var targetOldWorkflowQueue = reincarnatedOldWorkflowQ ?? oldWorkflowQ;

                    // It's good that we remove the workflows from the queue, but we really don't have to wait for them.
                    // Worst-case, it will be processed again by the old-queue.
                    targetOldWorkflowQueue.RemoveAllFromQueue(remainingWorkflows).Ignore();
                    return remainingWorkflows.Value.Select(w => w.WorkflowId);
                }
            }
            // If there are no remaining work-flow records, an empty Enumerable is returned.
            return Enumerable.Empty<Guid>();
        }

        private async Task<IIndexWorkflowQueue> GetReincarnatedWorkflowQueue(IIndexWorkflowQueue workflowQ)
        {
            var primaryKey = workflowQ.GetPrimaryKeyString();
            var reincarnatedQ = this._grainFactory.GetGrain<IIndexWorkflowQueue>(primaryKey);
            var reincarnatedQHandler = this._grainFactory.GetGrain<IIndexWorkflowQueueHandler>(primaryKey);
            await Task.WhenAll(reincarnatedQ.Initialize(workflowQ), reincarnatedQHandler.Initialize(workflowQ));
            return reincarnatedQ;
        }

        private Task PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(IEnumerable<Guid> workflowsInProgress)
        {
            var initialSize = this.ActiveWorkflowsSet.Count;
            this.ActiveWorkflowsSet.Clear();
            this.ActiveWorkflowsSet.UnionWith(workflowsInProgress);
            return (this.ActiveWorkflowsSet.Count != initialSize) ? base.writeGrainStateFunc() : Task.CompletedTask;
        }

        private void PruneWorkflowQueuesForMissingInterfaceTypes()
        {
            // Interface types may be missing if the grain definition was updated.
            var oldQueues = this.WorkflowQueues;
            this.WorkflowQueues = oldQueues.Where(kvp => base._grainIndexes.ContainsInterface(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public override Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsList()
        {
            var workflowIds = this.ActiveWorkflowsSet;

            // Immutable does not prevent items from being added to the hashset; there was a race condition where
            // IndexableGrain.ApplyIndexUpdates adds to the list after IndexWorkflowQueueHandlerBase.HandleWorkflowsUntilPunctuation
            // obtains grainsToActiveWorkflows and thus IndexWorkflowQueueHandlerBase.RemoveFromActiveWorkflowsInGrainsTasks
            // removes the added workflowId, which means that workflowId is not processed. Therefore deep-copy workflows.
            var result = (workflowIds == null) ? new HashSet<Guid>() : new HashSet<Guid>(workflowIds);
            return Task.FromResult(result.AsImmutable());
        }

        public override Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowIds)
        {
            if (this.ActiveWorkflowsSet != null && this.ActiveWorkflowsSet.RemoveWhere(g => removedWorkflowIds.Contains(g)) > 0)
            {
                // TODO: decide whether we need to actually write the state back to the storage or we can leave it for the next WriteStateAsync
                // on the grain itself.
                //return WriteBaseStateAsync();
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a workflow ID to the list of active workflows
        /// for this fault-tolerant indexable grain
        /// </summary>
        /// <param name="workflowId">the workflow ID to be added</param>
        private void AddWorkflowIdToActiveWorkflows(Guid workflowId)
        {
            if (this.ActiveWorkflowsSet == null)
            {
                this.ActiveWorkflowsSet = new HashSet<Guid>();
            }
            this.ActiveWorkflowsSet.Add(workflowId);
        }

        /// <summary>
        /// Generates a unique Guid that does not exist in the list of active workflows.
        /// 
        /// Actually, there is a very unlikely possibility that we end up with a duplicate workflow ID in the following scenario:
        /// 1- IndexableGrain G is updated and assigned workflow ID = A
        /// 2- workflow record with ID = A is added to the index workflow queue
        /// 3- G fails and its state (including its active workflow list) is thrown away
        /// 4- G is re-activated and reads it state from storage (which does not include A in its active workflow list)
        /// 5- G gets updated and a new workflow with ID = A is generated for it.
        ///    This ID is assumed to be unique, while it actually is not unique and already exists in the workflow queue.
        /// 
        /// The only way to avoid it is using a centralized unique workflow ID generator, which can be added if necessary.
        /// </summary>
        /// <returns>a new unique workflow ID</returns>
        private Guid GenerateUniqueWorkflowId()
        {
            var workflowId = Guid.NewGuid();
            while (this.ActiveWorkflowsSet != null && this.ActiveWorkflowsSet.Contains(workflowId))
            {
                workflowId = Guid.NewGuid();
            }
            return workflowId;
        }
    }
}
