using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// Interface for a grain interface that will "contain" indexed properties. This generic form is
    /// primarily used to extract <typeparamref name="TProperties"/> to obtain the property names for
    /// index naming and to create the TProperties ephemeral instance for index writing. TODO: Move to Orleans.Indexing.Facet\Interfaces
    /// </summary>
    /// <remarks>
    /// The indexed properties of an <see cref="IIndexableGrain{TProperties}"/> are actually
    /// the properties of <typeparamref name="TProperties"/>, not any properties of the interface itself.
    /// </remarks>
    public interface IIndexableGrain<TProperties> : IIndexableGrain where TProperties: new()
    {
    }

    /// <summary>
    /// Non-generic base interface for indexable grains; provides methods that allow the fault-tolerant indexing
    /// implementation to call back to the grain to retrieve and update the list of in-flight workflows.
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

        /// <summary>
        /// This method returns the set of active workflow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }
}
