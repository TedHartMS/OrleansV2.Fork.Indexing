using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Indexing.Facets
{
    internal class InterfaceToUpdatesMap: IEnumerable<KeyValuePair<Type, IDictionary<string, IMemberUpdate>>>
    {
        private readonly IDictionary<Type, IDictionary<string, IMemberUpdate>> updatesByInterface;

        internal IDictionary<string, IMemberUpdate> this[Type interfaceType] => this.updatesByInterface[interfaceType];

        internal InterfaceToUpdatesMap(IEnumerable<(Type interfaceType, IEnumerable<(string indexName, IMemberUpdate mu)> namedUpdates)> updateEnumerator)
        {
            this.updatesByInterface = updateEnumerator.Select(x => (itf: x.interfaceType, dict: x.namedUpdates.ToDictionary(upd => upd.indexName, upd => upd.mu)))
                                                      .Where(pair => pair.dict.Count > 0)
                                                      .ToDictionary(pair => pair.itf, pair => pair.dict as IDictionary<string, IMemberUpdate>);
        }

        internal bool IsEmpty => this.updatesByInterface.Count == 0;

        internal bool HasAnyDeletes => this.updatesByInterface.Values.Any(kvp => kvp.Values.Any(upd => upd.OperationType == IndexOperationType.Delete));

        public IEnumerator<KeyValuePair<Type, IDictionary<string, IMemberUpdate>>> GetEnumerator() => this.updatesByInterface.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
