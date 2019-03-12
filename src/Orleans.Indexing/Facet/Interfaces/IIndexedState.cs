using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// The base interface definition for a class that implements the indexing facet of a grain.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class of a <see cref="Grain{TGrainState}"/>.</typeparam>
    public interface IIndexedState<TGrainState> where TGrainState : new()
    {
        /// <summary>
        /// Attaches an <see cref="ITransactionalState{TState}"/> instance from the Grain's constructor.
        /// For transactional indexes only.
        /// </summary>
        void Attach(ITransactionalState<IndexedGrainStateWrapper<TGrainState>> transactionalState);

        /// <summary>
        /// Reads the grain state, which resets the value of all indexed and non-indexed properties.
        /// </summary>
        Task<TResult> PerformRead<TResult>(Func<TGrainState, TResult> readFunction);

        /// <summary>
        /// Executes <paramref name="updateFunction"/> then writes the grain state and the index entries for all indexed interfaces
        /// defined on the grain.
        /// </summary>
        Task<TResult> PerformUpdate<TResult>(Func<TGrainState, TResult> updateFunction);

        /// <summary>
        /// This method returns the set of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }
}
