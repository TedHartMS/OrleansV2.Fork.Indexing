using System;
using System.Collections.Generic;

namespace Orleans.Indexing
{
    internal class IndexRegistry
    {
        private IDictionary<Type, NamedIndexMap> IndexesByInterfaceType { get; set; } = new Dictionary<Type, NamedIndexMap>();

        private IDictionary<Type, Type[]> IndexesByGrainType = new Dictionary<Type, Type[]>();

        internal NamedIndexMap this[Type interfaceType]
        {
            get => this.IndexesByInterfaceType[interfaceType];
            set => this.IndexesByInterfaceType[interfaceType] = value;
        }

        internal bool TryGetValue(Type interfaceType, out NamedIndexMap interfaceIndexes)
            => this.IndexesByInterfaceType.TryGetValue(interfaceType, out interfaceIndexes);

        internal bool ContainsKey(Type interfaceType) => this.IndexesByInterfaceType.ContainsKey(interfaceType);

        internal void SetGrainIndexes(Type grainClassType, Type[] indexedInterfaces)
            => this.IndexesByGrainType[grainClassType] = indexedInterfaces;

        internal bool TryGetGrainIndexes(Type grainClassType, out Type[] indexedInterfaces)
            => this.IndexesByGrainType.TryGetValue(grainClassType, out indexedInterfaces);

        internal bool ContainsGrainType(Type grainClassType) => this.IndexesByGrainType.ContainsKey(grainClassType);
    }
}
