using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Orleans.Indexing.Tests
{
    internal class TestMultiIndexGrainBase<TState, TProps> where TProps: new()
    {
        internal bool IsUniqueIntIndexed;
        internal bool IsUniqueStringIndexed;
        internal bool IsNonUniqueIntIndexed;
        internal bool IsNonUniqueStringIndexed;

        private Func<Task> writeStateFunc;
        private Func<Task> readStateFunc;

        internal TestMultiIndexGrainBase(Func<Task> wsf, Func<Task> rsf)
        {
            this.writeStateFunc = wsf;
            this.readStateFunc = rsf;

            Type propertiesType = typeof(TProps);
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

            Assert.True(IsUniqueIntIndexed || IsUniqueStringIndexed || IsNonUniqueIntIndexed || IsNonUniqueStringIndexed);
        }

        internal async Task SetProperty<T>(Action<T> setter, T value, bool retry)
        {
            await IndexingTestUtils.SetProperty(setter, value, this.writeStateFunc, this.readStateFunc, retry);
        }

        internal Task SetPropertyWithoutWrite<T>(Action<T> setter, T value)
        {
            setter(value);
            return Task.CompletedTask;
        }
    }
}
