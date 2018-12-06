using Orleans.Providers;
using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public abstract class TestMultiIndexGrain<TState, TProps> : IndexableGrain<TState, TProps>, ITestMultiIndexGrain
        where TState : class, ITestMultiIndexState, new() where TProps : ITestMultiIndexProperties, new()
    {
        private TestMultiIndexGrainBase<TState, TProps> testBase;

        public Task<string> GetUnIndexedString() => Task.FromResult(this.State.UnIndexedString);
        public Task SetUnIndexedString(string value) => testBase.SetProperty(v => this.State.UnIndexedString = v, value, retry:false);

        public Task<int> GetUniqueInt() => Task.FromResult(this.State.UniqueInt);
        public Task SetUniqueInt(int value) => testBase.SetProperty(v => this.State.UniqueInt = v, value, retry:testBase.IsUniqueIntIndexed);

        public Task<string> GetUniqueString() => Task.FromResult(this.State.UniqueString);
        public Task SetUniqueString(string value) => testBase.SetProperty(v => this.State.UniqueString = v, value, retry:testBase.IsUniqueStringIndexed);

        public Task<int> GetNonUniqueInt() => Task.FromResult(this.State.NonUniqueInt);
        public Task SetNonUniqueInt(int value) => testBase.SetProperty(v => this.State.NonUniqueInt = v, value, retry:testBase.IsNonUniqueIntIndexed);

        public Task<string> GetNonUniqueString() => Task.FromResult(this.State.NonUniqueString);
        public Task SetNonUniqueString(string value) => testBase.SetProperty(v => this.State.NonUniqueString = v, value, retry:testBase.IsNonUniqueStringIndexed);

        public Task Deactivate()
        {
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public TestMultiIndexGrain()
        {
            this.testBase = new TestMultiIndexGrainBase<TState, TProps>(() => base.WriteStateAsync(), () => base.ReadStateAsync());
        }
    }
}
