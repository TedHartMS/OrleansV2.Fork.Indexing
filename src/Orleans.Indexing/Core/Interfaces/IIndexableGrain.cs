using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Marker interface for an interface that will "contain" indexed properties. TODO: MOve to Orleans.Indexing.Facets
    /// </summary>
    /// <remarks>
    /// The indexed properties of an <see cref="IIndexableGrain{TProperties}"/> are actually
    /// the properties of <typeparamref name="TProperties"/>, not any properties of the interface itself.
    /// </remarks>
    public interface IIndexableGrain<TProperties> : IIndexableGrain
    {
    }

    /// <summary>
    /// Untyped base marker interface for indexable grains.
    /// </summary>
    public interface IIndexableGrain : IGrain
    {
        /// <summary>
        /// Extracts the corresponding image of grain for a particular indexidentified by iUpdateGen.
        /// 
        /// IIndexUpdateGenerator should always be applied inside the grain implementation, as it might
        /// contain blocking code, and it is not intended to be called externally.
        /// </summary>
        /// <param name="iUpdateGen">IIndexUpdateGenerator for a particular index</param>
        /// <returns>the corresponding image of grain for a particular index</returns>
        Task<object> ExtractIndexImage(IIndexUpdateGenerator iUpdateGen);

        /// <summary>
        /// This method returns the list of active work-flow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsList();

        /// <summary>
        /// This method removes a work-flow ID from the list of active work-flow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }
}
