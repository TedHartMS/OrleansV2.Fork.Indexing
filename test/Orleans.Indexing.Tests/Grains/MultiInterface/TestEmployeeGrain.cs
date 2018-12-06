using Orleans.Providers;
using System.Threading.Tasks;
using Orleans.Indexing.Facets;
using System;

namespace Orleans.Indexing.Tests.MultiInterface
{
    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public abstract class TestEmployeeGrain<TGrainState, TWrappedState> : Grain<TWrappedState>
        where TGrainState : class, IEmployeeGrainState, new()
        where TWrappedState: IndexableGrainStateWrapper<TGrainState>, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces on this class.
        private readonly IIndexWriter<TGrainState> indexWriter;

        IEmployeeGrainState UnwrappedState => base.State.UserState;

        // This illustrates implementing the Grain interfaces to get and set the properties.
        #region IPersonInterface
        public Task<string> GetLocation() => Task.FromResult(this.UnwrappedState.Location);
        public Task SetLocation(string value) { this.UnwrappedState.Location = value; return Task.CompletedTask; }

        public Task<int> GetAge() => Task.FromResult(this.UnwrappedState.Age);
        public Task SetAge(int value) { this.UnwrappedState.Age = value; return Task.CompletedTask; }
        #endregion IPersonInterface

        #region IJobInterface
        public Task<string> GetTitle() => Task.FromResult(this.UnwrappedState.Title);
        public Task SetTitle(string value) { this.UnwrappedState.Title = value; return Task.CompletedTask; }

        public Task<string> GetDepartment() => Task.FromResult(this.UnwrappedState.Department);
        public Task SetDepartment(string value) { this.UnwrappedState.Department = value; return Task.CompletedTask; }
        #endregion IJobInterface

        #region IEmployeeState
        public Task<int> GetTenure() => Task.FromResult(this.UnwrappedState.Tenure);
        public Task SetTenure(int value) { this.UnwrappedState.Tenure = value; return Task.CompletedTask; }
        #endregion IEmployeeState

        public Task WriteState() => this.WriteStateAsync();

        public Task Deactivate() { base.DeactivateOnIdle(); return Task.CompletedTask; }

        protected TestEmployeeGrain(IIndexWriter<TGrainState> indexWriter) => this.indexWriter = indexWriter;

        #region Facet methods - required overrides of Grain<TGrainState>
        public override Task OnActivateAsync() => this.indexWriter.OnActivateAsync(this, base.State, () => base.WriteStateAsync(), () => Task.CompletedTask);
        public override Task OnDeactivateAsync() => this.indexWriter.OnDeactivateAsync(() => Task.CompletedTask);
        protected override Task WriteStateAsync() => this.indexWriter.WriteAsync();
        #endregion Facet methods - required overrides of Grain<TGrainState>

        // TODO remove when facetization complete; for now it just makes the compiler happy
        public Task<object> ExtractIndexImage(IIndexUpdateGenerator iUpdateGen) => throw new NotImplementedException();

        // TODO: try to remove this shim from the user-visible grain
        public Task<Concurrency.Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexWriter.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexWriter.RemoveFromActiveWorkflowIds(removedWorkflowId);
    }
}
