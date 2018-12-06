using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Concurrency;

namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// The base interface definition for a class that implements the indexing facet of a grain.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class of a <see cref="Grain{TGrainState}"/>.</typeparam>
    public interface IIndexWriter<TGrainState> where TGrainState : new()
    {
        /// <summary>
        /// The <see cref="Grain.OnActivateAsync()"/> implementation must call this; in turn, this
        /// calls <paramref name="writeGrainStateFunc"/> if needed and then <paramref name="onGrainActivateFunc"/>, in
        /// which the grain implementation should do any additional activation logic if desired.
        /// </summary>
        /// <param name="grain">The grain to manage indexes for</param>
        /// <param name="wrappedState">The state for <paramref name="grain"/>, which is either the base grain state from
        ///     <see cref="Grain{TGrainState}"/> or a local state member if <paramref name="grain"/> inherits directly
        ///     from <see cref="Grain"/>.</param>
        /// <param name="writeGrainStateFunc">If <paramref name="grain"/>" inherits from <see cref="Grain{TGrainState}"/>,
        ///     this is usually a lambda that calls "base.WriteStateAsync()". Otherwise it is either a custom persistence
        ///     call or simply "() => Task.CompletedTask". It is called in parallel with inserting grain update operations
        ///     into the silo index workflows.</param>
        /// <param name="onGrainActivateFunc">If <paramref name="grain"/> implements custom activation logic, it supplies
        ///     a lambda to do so here, or may simply pass "() => Task.CompletedTask". It is called in parallel with
        ///     inserting grain indexes into the silo index collections and later during <see cref="WriteAsync()"/>.</param>
        Task OnActivateAsync(Grain grain, IndexableGrainStateWrapper<TGrainState> wrappedState, Func<Task> writeGrainStateFunc,
                             Func<Task> onGrainActivateFunc);

        /// <summary>
        /// The <see cref="Grain.OnDeactivateAsync()"/> implementation must call this; in turn, this
        /// calls <paramref name="onGrainDeactivateFunc"/>, in which the grain implementation should
        /// do any additional deactivation logic, if desired.
        /// </summary>
        /// <param name="onGrainDeactivateFunc">If the grain implements custom deactivation logic, it supplies
        ///     a lambda to do so here, or may simply pass "() => Task.CompletedTask". It is called in parallel with
        ///     removing grain indexes from the silo index collections.</param>
        Task OnDeactivateAsync(Func<Task> onGrainDeactivateFunc);

        /// <summary>
        /// The <see cref="Grain{TGrainState}.WriteStateAsync()"/> implementation must call this; in turn, this calls
        /// onGrainActivateFunc passed to <see cref="OnActivateAsync(Grain, IndexableGrainStateWrapper{TGrainState}, Func{Task}, Func{Task})"/> 
        /// in parallel with inserting grain update operations into the silo index workflows.
        /// </summary>
        /// <remarks>
        /// Coordinates the writing of all indexed interfaces defined on the grain. It will retrieve this from cached
        /// per-grain-class list of indexes and properties to do the mapping, and maps the State structure to the various
        /// TProperties structures. If workflow-based, it includes the grain state update in the workflow appropriately.
        /// </remarks>
        Task WriteAsync();

        #region TODO obsolete; remove
        /// <summary>
        /// This method returns the set of active work-flow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a work-flow ID from the list of active work-flow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
        #endregion TODO obsolete; remove
    }
}
