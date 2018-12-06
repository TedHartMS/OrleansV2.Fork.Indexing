using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    internal class IndexRegistry
    {
        private IDictionary<Type, NamedIndexMap> IndexesByInterfaceType { get; set; } = new Dictionary<Type, NamedIndexMap>();

        private IDictionary<Type, Type[]> IndexesByGrainType = new Dictionary<Type, Type[]>();

        private IDictionary<Type, IReadOnlyDictionary<string, object>> GrainsToPropertyNullValues = new Dictionary<Type, IReadOnlyDictionary<string, object>>();

        internal static IReadOnlyDictionary<string, object> EmptyPropertyNullValues { get; } = new Dictionary<string, object>();

        internal NamedIndexMap this[Type interfaceType]
        {
            get => this.IndexesByInterfaceType[interfaceType];
            set => this.IndexesByInterfaceType[interfaceType] = value;
        }

        internal bool TryGetValue(Type interfaceType, out NamedIndexMap interfaceIndexes)
            => this.IndexesByInterfaceType.TryGetValue(interfaceType, out interfaceIndexes);

        internal bool ContainsKey(Type interfaceType) => this.IndexesByInterfaceType.ContainsKey(interfaceType);

        internal void SetGrainIndexes(Type grainClassType, Type[] indexedInterfaces, IReadOnlyDictionary<string, object> nullValuesDictionary)
        { 
            this.IndexesByGrainType[grainClassType] = indexedInterfaces;
            this.GrainsToPropertyNullValues[grainClassType] = nullValuesDictionary;
        }

        internal bool TryGetGrainIndexes(Type grainClassType, out Type[] indexedInterfaces)
            => this.IndexesByGrainType.TryGetValue(grainClassType, out indexedInterfaces);

        internal IReadOnlyDictionary<string, object> GetNullPropertyValuesForGrain(Type grainClassType)
            => this.GrainsToPropertyNullValues.TryGetValue(grainClassType, out var nullValuesDict) ? nullValuesDict : EmptyPropertyNullValues;

        internal bool ContainsGrainType(Type grainClassType) => this.IndexesByGrainType.ContainsKey(grainClassType);
    }
}
