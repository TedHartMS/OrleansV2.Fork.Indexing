using System;
using System.Threading.Tasks;

namespace Orleans.Indexing.Facets
{
    public interface IIndexWriter<TGrainState> where TGrainState : new()
    {
        /// <summary>
        /// Coordinates the writing of all indexed interfaces defined on the grain. It will retrieve this from cached
        /// per-grain-class list of indexes and properties to do the mapping, and maps the State structure to the various
        /// TProperties structures. If workflow-based, it includes the grain state update in the workflow appropriately.
        /// </summary>
        /// <param name="grain">The Grain, from which the list of indexable interfaces and state data are obtained.</param>
        /// <param name="stateUpdateAction">The grain state update operation; usually () => base.WriteStateAsync().</param>
        /// <returns></returns>
        Task Write(Grain<TGrainState> grain, Func<Task> stateUpdateAction);
    }
}
