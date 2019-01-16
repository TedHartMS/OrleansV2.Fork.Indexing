using Orleans.Concurrency;
using Orleans.Indexing.Facet;
using Orleans.Providers;
using System;
using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    public abstract class TestMultiIndexGrainNonFaultTolerant<TGrainState> : TestMultiIndexGrain<TGrainState, IndexableGrainStateWrapper<TGrainState>>
        where TGrainState : class, ITestMultiIndexState, new()
    {
        public TestMultiIndexGrainNonFaultTolerant(IIndexWriter<TGrainState> indexWriter) : base(indexWriter) { }
    }

    public abstract class TestMultiIndexGrainFaultTolerant<TGrainState> : TestMultiIndexGrain<TGrainState, FaultTolerantIndexableGrainStateWrapper<TGrainState>>
        where TGrainState : class, ITestMultiIndexState, new()
    {
        public TestMultiIndexGrainFaultTolerant(IIndexWriter<TGrainState> indexWriter) : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public abstract class TestMultiIndexGrain<TGrainState, TWrappedState> : Grain<TWrappedState>, ITestMultiIndexGrain
        where TGrainState : class, ITestMultiIndexState, new()
        where TWrappedState : IndexableGrainStateWrapper<TGrainState>, new()
    {
        private readonly TestMultiIndexGrainBase<TGrainState> testBase;
        private TGrainState unwrappedState => base.State.UserState;

        public Task<string> GetUnIndexedString() => Task.FromResult(this.unwrappedState.UnIndexedString);
        public Task SetUnIndexedString(string value) => this.testBase.SetProperty(() => this.unwrappedState.UnIndexedString = value, retry:false);

        public Task<int> GetUniqueInt() => Task.FromResult(this.unwrappedState.UniqueInt);
        public Task SetUniqueInt(int value) => this.testBase.SetProperty(() => this.unwrappedState.UniqueInt = value, retry: this.testBase.IsUniqueIntIndexed);

        public Task<string> GetUniqueString() => Task.FromResult(this.unwrappedState.UniqueString);
        public Task SetUniqueString(string value) => this.testBase.SetProperty(() => this.unwrappedState.UniqueString = value, retry: this.testBase.IsUniqueStringIndexed);

        public Task<int> GetNonUniqueInt() => Task.FromResult(this.unwrappedState.NonUniqueInt);
        public Task SetNonUniqueInt(int value) => this.testBase.SetProperty(() => this.unwrappedState.NonUniqueInt = value, retry: this.testBase.IsNonUniqueIntIndexed);

        public Task<string> GetNonUniqueString() => Task.FromResult(this.unwrappedState.NonUniqueString);
        public Task SetNonUniqueString(string value) => this.testBase.SetProperty(() => this.unwrappedState.NonUniqueString = value, retry: this.testBase.IsNonUniqueStringIndexed);

        public Task Deactivate()
        {
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public TestMultiIndexGrain(IIndexWriter<TGrainState> indexWriter) =>
            this.testBase = new TestMultiIndexGrainBase<TGrainState>(this.GetType(), indexWriter, this.WriteStateAsync, this.ReadStateAsync);

        #region Facet methods - required overrides of Grain<TGrainState>
        public override Task OnActivateAsync() => this.testBase.IndexWriter.OnActivateAsync(this, base.State, base.WriteStateAsync, base.OnActivateAsync);
        public override Task OnDeactivateAsync() => this.testBase.IndexWriter.OnDeactivateAsync(() => Task.CompletedTask);
        protected override Task WriteStateAsync() => this.testBase.IndexWriter.WriteAsync();
        #endregion Facet methods - required overrides of Grain<TGrainState>

        #region Required shims for IIndexableGrain methods for fault tolerance
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.testBase.IndexWriter.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.testBase.IndexWriter.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion Required shims for IIndexableGrain methods for fault tolerance
    }
}
