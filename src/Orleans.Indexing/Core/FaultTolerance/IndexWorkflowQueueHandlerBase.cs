using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    internal class IndexWorkflowQueueHandlerBase : IIndexWorkflowQueueHandler
    {
        private IIndexWorkflowQueue __workflowQueue;
        private IIndexWorkflowQueue WorkflowQueue => __workflowQueue ?? InitIndexWorkflowQueue();

        private int _queueSeqNum;
        private Type _iGrainType;

        private bool _isDefinedAsFaultTolerantGrain;
        private bool _hasAnyTotalIndex;
        private bool HasAnyTotalIndex { get { EnsureGrainIndexes(); return _hasAnyTotalIndex; } }
        private bool IsFaultTolerant => _isDefinedAsFaultTolerantGrain && HasAnyTotalIndex;

        private NamedIndexMap __grainIndexes;

        private NamedIndexMap GrainIndexes => EnsureGrainIndexes();

        private SiloAddress _silo;
        private SiloIndexManager _siloIndexManager;
        private Lazy<GrainReference> _lazyParent;

        internal IndexWorkflowQueueHandlerBase(SiloIndexManager siloIndexManager, Type iGrainType, int queueSeqNum, SiloAddress silo,
                                               bool isDefinedAsFaultTolerantGrain, Func<GrainReference> parentFunc)
        {
            _iGrainType = iGrainType;
            _queueSeqNum = queueSeqNum;
            _isDefinedAsFaultTolerantGrain = isDefinedAsFaultTolerantGrain;
            _hasAnyTotalIndex = false;
            __grainIndexes = null;
            __workflowQueue = null;
            _silo = silo;
            _siloIndexManager = siloIndexManager;
            _lazyParent = new Lazy<GrainReference>(parentFunc, true);
        }

        public async Task HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> workflowRecords)
        {
            try
            {
                var workflowNode = workflowRecords.Value;
                while (workflowNode != null)
                {
                    var grainsToActiveWorkflows = IsFaultTolerant ? await GetActiveWorkflowsListsFromGrains(workflowNode) : emptyDictionary;
                    var updatesToIndexes = PopulateUpdatesToIndexes(workflowNode, grainsToActiveWorkflows);

                    await Task.WhenAll(PrepareIndexUpdateTasks(updatesToIndexes));
                    if (this.IsFaultTolerant)
                    {
                        Task.WhenAll(this.RemoveFromActiveWorkflowsInGrainsTasks(grainsToActiveWorkflows)).Ignore();
                    }
                    workflowNode = (await this.WorkflowQueue.GiveMoreWorkflowsOrSetAsIdle()).Value;
                }
            }
            catch (Exception e)
            {
                throw e;    // TODO empty handler; add handler logic or remove
            }
        }

        private IEnumerable<Task> RemoveFromActiveWorkflowsInGrainsTasks(Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows)
            => grainsToActiveWorkflows.Select(kvp => kvp.Key.RemoveFromActiveWorkflowIds(kvp.Value));

        private IEnumerable<Task<bool>> PrepareIndexUpdateTasks(Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> updatesToIndexes)
            => this.GrainIndexes.Select(indexEntry => (indexInfo: indexEntry.Value, updatesToIndex: updatesToIndexes[indexEntry.Key]))
                                .Where(pair => pair.updatesToIndex.Count > 0)
                                .Select(pair => pair.indexInfo.IndexInterface.ApplyIndexUpdateBatch(this._siloIndexManager, pair.updatesToIndex.AsImmutable(),
                                                                                pair.indexInfo.MetaData.IsUniqueIndex, pair.indexInfo.MetaData, _silo));

        private Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> PopulateUpdatesToIndexes(
                        IndexWorkflowRecordNode currentWorkflow, Dictionary<IIndexableGrain, HashSet<Guid>> grainsToActiveWorkflows)
        {
            var updatesToIndexes = CreateAMapForUpdatesToIndexes();
            bool faultTolerant = IsFaultTolerant;
            while (!currentWorkflow.IsPunctuation())
            {
                IndexWorkflowRecord workflowRec = currentWorkflow.WorkflowRecord;
                IIndexableGrain g = workflowRec.Grain;
                bool existsInActiveWorkflows = faultTolerant && grainsToActiveWorkflows.TryGetValue(g, out HashSet<Guid> activeWorkflowRecs)
                                                             && activeWorkflowRecs.Contains(workflowRec.WorkflowId);

                foreach (var (indexName, updt) in currentWorkflow.WorkflowRecord.MemberUpdates.Where(kvp => kvp.Value.OperationType != IndexOperationType.None))
                {
                    var updatesToIndex = updatesToIndexes[indexName];
                    IList<IMemberUpdate> updatesList = updatesToIndex.GetOrAdd(g, () => new List<IMemberUpdate>());

                    System.Diagnostics.Debug.Assert(!faultTolerant || existsInActiveWorkflows, "TODO inactive FT record; for current tests this indicates a race condition");

                    if (!faultTolerant || existsInActiveWorkflows)
                    {
                        updatesList.Add(updt);
                    }
                    else if (GrainIndexes[indexName].MetaData.IsUniqueIndex)
                    {
                        // TODO: Race condition here where FTWIndexWriter has not yet completed its eager write of unique indexes
                        //       and thus has not placed the workflowId into its active list.

                        // If the workflow record does not exist in the list of active work-flows and the index is fault-tolerant,
                        // enqueue a reversal (undo) to any possible remaining tentative updates to unique indexes.
                        updatesList.Add(new MemberUpdateReverseTentative(updt));
                    }
                }
                currentWorkflow = currentWorkflow.Next;
            }
            return updatesToIndexes;
        }

        private static HashSet<Guid> emptyHashset = new HashSet<Guid>();
        private static Dictionary<IIndexableGrain, HashSet<Guid>> emptyDictionary = new Dictionary<IIndexableGrain, HashSet<Guid>>();

        private async Task<Dictionary<IIndexableGrain, HashSet<Guid>>> GetActiveWorkflowsListsFromGrains(IndexWorkflowRecordNode currentWorkflow)
        {
            var result = new Dictionary<IIndexableGrain, HashSet<Guid>>();
            var grains = new List<IIndexableGrain>();
            var activeWorkflowsSetsTasks = new List<Task<Immutable<HashSet<Guid>>>>();
            var workflowIds = new HashSet<Guid>();

            while (!currentWorkflow.IsPunctuation())
            {
                workflowIds.Add(currentWorkflow.WorkflowRecord.WorkflowId);
                IIndexableGrain g = currentWorkflow.WorkflowRecord.Grain;
                foreach (var updates in currentWorkflow.WorkflowRecord.MemberUpdates)
                {
                    IMemberUpdate updt = updates.Value;
                    if (updt.OperationType != IndexOperationType.None && !result.ContainsKey(g))
                    {
                        result.Add(g, emptyHashset);
                        grains.Add(g);
                        activeWorkflowsSetsTasks.Add(g.AsReference<IIndexableGrain>(_siloIndexManager, _iGrainType).GetActiveWorkflowIdsList());
                    }
                }
                currentWorkflow = currentWorkflow.Next;
            }

            if (activeWorkflowsSetsTasks.Count() > 0)
            {
                Immutable<HashSet<Guid>>[] activeWorkflowsSets = await Task.WhenAll(activeWorkflowsSetsTasks);
                for (int i = 0; i < activeWorkflowsSets.Length; ++i)
                {
                    // Do not include workflowIds that are not in our work queue.
                    result[grains[i]] = new HashSet<Guid>(activeWorkflowsSets[i].Value.Intersect(workflowIds));
                }
            }

            return result;
        }

        private Dictionary<string, IDictionary<IIndexableGrain, IList<IMemberUpdate>>> CreateAMapForUpdatesToIndexes()
            => GrainIndexes.Keys.Select(key => new { key, dict = new Dictionary<IIndexableGrain, IList<IMemberUpdate>>() as IDictionary<IIndexableGrain, IList<IMemberUpdate>> })
                                .ToDictionary(it => it.key, it => it.dict);

        private NamedIndexMap EnsureGrainIndexes()
        {
            if (__grainIndexes == null)
            {
                __grainIndexes = _siloIndexManager.IndexFactory.GetGrainIndexes(_iGrainType);
                _hasAnyTotalIndex = __grainIndexes.HasAnyTotalIndex;
            }
            return __grainIndexes;
        }

        private IIndexWorkflowQueue InitIndexWorkflowQueue()
            => __workflowQueue = _lazyParent.Value.IsGrainService
                    ? _siloIndexManager.GetGrainService<IIndexWorkflowQueue>(IndexWorkflowQueueBase.CreateIndexWorkflowQueueGrainReference(_siloIndexManager, _iGrainType, _queueSeqNum, _silo))
                    : _siloIndexManager.GrainFactory.GetGrain<IIndexWorkflowQueue>(IndexWorkflowQueueBase.CreateIndexWorkflowQueuePrimaryKey(_iGrainType, _queueSeqNum));

        public static GrainReference CreateIndexWorkflowQueueHandlerGrainReference(SiloIndexManager siloIndexManager, Type grainInterfaceType, int queueSeqNum, SiloAddress siloAddress)
            => siloIndexManager.MakeGrainServiceGrainReference(IndexingConstants.INDEX_WORKFLOW_QUEUE_HANDLER_GRAIN_SERVICE_TYPE_CODE,
                                                               IndexWorkflowQueueBase.CreateIndexWorkflowQueuePrimaryKey(grainInterfaceType, queueSeqNum), siloAddress);

        public Task Initialize(IIndexWorkflowQueue oldParentGrainService)
            => throw new NotSupportedException();
    }
}
