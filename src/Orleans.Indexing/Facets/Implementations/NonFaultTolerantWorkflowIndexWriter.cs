using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class NonFaultTolerantWorkflowIndexWriter<TGrainState> : WorkflowIndexWriterBase<TGrainState>,
                                                                    INonFaultTolerantWorkflowIndexWriter<TGrainState> where TGrainState : class, new()
    {
        public NonFaultTolerantWorkflowIndexWriter(
                IServiceProvider sp,
                IIndexWriterConfiguration config
            ) : base(sp, config)
        {
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
                                                                bool updateIndexesEagerly,
                                                                bool onlyUniqueIndexesWereUpdated,
                                                                int numberOfUniqueIndexesUpdated,
                                                                bool writeStateIfConstraintsAreNotViolated)
        {
            // If there is no update to the indexes, we should only write back the state of the grain, if requested.
            if (updatesByInterface.IsEmpty)
            {
                if (writeStateIfConstraintsAreNotViolated)
                {
                    await base.writeGrainStateFunc();
                }
                return;
            }

            // HashIndexBucketState will not actually perform an index removal (Delete) if the index is not marked tentative.
            // Therefore we must do a two-step approach here; mark a tentative Delete, then do the non-tentative Delete.
            var updateEagerUniqueIndexesTentatively = numberOfUniqueIndexesUpdated > 1 || updatesByInterface.HasAnyDeletes;

            // Apply any unique index updates eagerly.
            if (numberOfUniqueIndexesUpdated > 0)
            {
                try
                {
                    // If there is more than one unique index to update, then updates to the unique indexes should be tentative
                    // so they are not visible to readers before making sure that all uniqueness constraints are satisfied.
                    await this.ApplyIndexUpdatesEagerly(updatesByInterface, UpdateIndexType.Unique, updateEagerUniqueIndexesTentatively);
                }
                catch (UniquenessConstraintViolatedException ex)
                {
                    // If any uniqueness constraint is violated and we have more than one unique index defined, then all tentative
                    // updates must be undone, then the exception is thrown back to the user code.
                    if (updateEagerUniqueIndexesTentatively)
                    {
                        await this.UndoTentativeChangesToUniqueIndexesEagerly(updatesByInterface);
                    }
                    throw ex;
                }
            }

            if (updateIndexesEagerly)
            {
                var updateIndexTypes = UpdateIndexType.None;
                if (updateEagerUniqueIndexesTentatively) updateIndexTypes |= UpdateIndexType.Unique;
                if (!onlyUniqueIndexesWereUpdated) updateIndexTypes |= UpdateIndexType.NonUnique;

                if (updateIndexTypes != UpdateIndexType.None || writeStateIfConstraintsAreNotViolated)
                {
                    var tasks = new List<Task>();

                    if (updateIndexTypes != UpdateIndexType.None)
                        tasks.Add(base.ApplyIndexUpdatesEagerly(updatesByInterface, updateIndexTypes, updateIndexesTentatively: false));
                    if (writeStateIfConstraintsAreNotViolated)
                        tasks.Add(base.writeGrainStateFunc());

                    await Task.WhenAll(tasks);
                }
            }
            else // !updateIndexesEagerly
            {
                this.ApplyIndexUpdatesLazilyWithoutWait(updatesByInterface, Guid.NewGuid());
                if (writeStateIfConstraintsAreNotViolated)
                {
                    await base.writeGrainStateFunc();
                }
            }

            // If everything was successful, the before images are updated
            this.UpdateBeforeImages(updatesByInterface);
        }

        private Task UndoTentativeChangesToUniqueIndexesEagerly(InterfaceToUpdatesMap updatesByInterface)
        {
            return Task.WhenAll(updatesByInterface.Select(kvp =>
                            base.ApplyIndexUpdatesEagerly(kvp.Key, MemberUpdateReverseTentative.Reverse(kvp.Value),
                                                          UpdateIndexType.Unique, updateIndexesTentatively: false)));
        }

        /// <summary>
        /// Lazily Applies updates to the indexes defined on this grain
        /// 
        /// The lazy update involves adding a work-flow record to the corresponding IIndexWorkflowQueue for this grain.
        /// </summary>
        /// <param name="updatesByInterface">the dictionary of updates for each index by interface</param>
        /// <param name="workflowID">the workflow identifier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyIndexUpdatesLazilyWithoutWait(InterfaceToUpdatesMap updatesByInterface, Guid workflowID)
        {
            base.ApplyIndexUpdatesLazily(updatesByInterface, workflowID).Ignore();
        }
    }
}
