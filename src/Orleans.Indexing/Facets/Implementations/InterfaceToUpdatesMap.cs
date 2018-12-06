using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Indexing.Facets
{
    internal class InterfaceToUpdatesMap: IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<string, IMemberUpdate>>>
    {
        private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IMemberUpdate>> updatesByInterface;
        internal IReadOnlyDictionary<Type, Guid> WorkflowIds;

        internal IReadOnlyDictionary<string, IMemberUpdate> this[Type interfaceType] => this.updatesByInterface[interfaceType];

        internal InterfaceToUpdatesMap(IEnumerable<(Type interfaceType, IEnumerable<(string indexName, IMemberUpdate mu)> namedUpdates)> updateEnumerator,
                                       Func<Guid> getWorkflowIdFunc)
        {
            this.updatesByInterface = updateEnumerator.Select(x => (itf: x.interfaceType, dict: x.namedUpdates.ToDictionary(upd => upd.indexName, upd => upd.mu)))
                                                      .Where(pair => pair.dict.Count > 0)
                                                      .ToDictionary(pair => pair.itf, pair => pair.dict as IReadOnlyDictionary<string, IMemberUpdate>);
            this.WorkflowIds = this.updatesByInterface.Keys.ToDictionary(key => key, _ => getWorkflowIdFunc()) as IReadOnlyDictionary<Type, Guid>;
         }

        internal bool IsEmpty => this.updatesByInterface.Count == 0;

        internal bool HasAnyDeletes => this.updatesByInterface.Values.Any(kvp => kvp.Values.Any(upd => upd.OperationType == IndexOperationType.Delete));

        public IEnumerator<KeyValuePair<Type, IReadOnlyDictionary<string, IMemberUpdate>>> GetEnumerator() => this.updatesByInterface.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
