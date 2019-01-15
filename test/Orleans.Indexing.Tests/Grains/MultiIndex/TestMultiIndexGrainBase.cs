using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    internal class TestMultiIndexGrainBase<TGrainState> where TGrainState : class, ITestMultiIndexState, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces on this class.
        internal readonly IIndexWriter<TGrainState> IndexWriter;

        internal bool IsUniqueIntIndexed;
        internal bool IsUniqueStringIndexed;
        internal bool IsNonUniqueIntIndexed;
        internal bool IsNonUniqueStringIndexed;

        private readonly Func<Task> writeStateFunc;
        private readonly Func<Task> readStateFunc;

        internal TestMultiIndexGrainBase(Type grainClassType, IIndexWriter<TGrainState> writer, Func<Task> wsf, Func<Task> rsf)
        {
            this.IndexWriter = writer;
            this.writeStateFunc = wsf;
            this.readStateFunc = rsf;

            var grainInterfaceTypes = ApplicationPartsIndexableGrainLoader.EnumerateIndexedInterfacesForAGrainClassType(grainClassType).ToArray();
            Assert.Single(grainInterfaceTypes);
            var propertiesType = grainInterfaceTypes[0].propertiesType;
            Assert.True(propertiesType.IsClass);

            bool isIndexed(string propertyName)
            {
                var propInfo = propertiesType.GetProperty(propertyName);
                return propInfo.GetCustomAttributes<IndexAttribute>(inherit: false).Any();
            }

            this.IsUniqueIntIndexed = isIndexed(nameof(ITestMultiIndexProperties.UniqueInt));
            this.IsUniqueStringIndexed = isIndexed(nameof(ITestMultiIndexProperties.UniqueString));
            this.IsNonUniqueIntIndexed = isIndexed(nameof(ITestMultiIndexProperties.NonUniqueInt));
            this.IsNonUniqueStringIndexed = isIndexed(nameof(ITestMultiIndexProperties.NonUniqueString));

            Assert.True(this.IsUniqueIntIndexed || this.IsUniqueStringIndexed || this.IsNonUniqueIntIndexed || this.IsNonUniqueStringIndexed);
        }

        internal async Task SetProperty(Action setterAction, bool retry)
            => await IndexingTestUtils.SetPropertyAndWriteStateAsync(setterAction, this.writeStateFunc, this.readStateFunc, retry);
    }
}
