using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Marker interface for an interface that will "contain" indexed properties. This generic form is
    /// primarily used as a marker to extract <typeparamref name="TProperties"/> to obtain the property
    /// names for index naming and. TODO: Move to Orleans.Indexing.Facets\Interfaces
    /// </summary>
    /// <remarks>
    /// The indexed properties of an <see cref="IIndexableGrain{TProperties}"/> are actually
    /// the properties of <typeparamref name="TProperties"/>, not any properties of the interface itself.
    /// </remarks>
    public interface IIndexableGrain<TProperties> : IIndexableGrain where TProperties: new()
    {
    }

    /// <summary>
    /// Untyped base marker interface for indexable grains.
    /// </summary>
    public interface IIndexableGrain : IGrain
    {
        #region TODO obsolete; in Facet it's moved elsewhere
        /// <summary>
        /// Extracts the corresponding image of grain for a particular indexidentified by iUpdateGen.
        /// 
        /// IIndexUpdateGenerator should always be applied inside the grain implementation, as it might
        /// contain blocking code, and it is not intended to be called externally.
        /// </summary>
        /// <param name="iUpdateGen">IIndexUpdateGenerator for a particular index</param>
        /// <returns>the corresponding image of grain for a particular index</returns>
        Task<object> ExtractIndexImage(IIndexUpdateGenerator iUpdateGen);
        #endregion TODO obsolete; in Facet it's moved elsewhere

        #region TODO: Maybe obsolete
        /// <summary>
        /// This method returns the set of active work-flow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a work-flow ID from the list of active work-flow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
        #endregion TODO Maybe obsolete
    }
}
