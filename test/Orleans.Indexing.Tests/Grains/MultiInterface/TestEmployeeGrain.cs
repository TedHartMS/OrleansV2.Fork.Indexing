using Orleans.Providers;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Indexing.Facet;
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

        private IEmployeeGrainState unwrappedState => base.State.UserState;

        // This illustrates implementing the Grain interfaces to get and set the properties.
        #region IPersonInterface
        public Task<string> GetName() => Task.FromResult(this.unwrappedState.Name);
        public Task SetName(string value) { this.unwrappedState.Name = value; return Task.CompletedTask; }

        public Task<int> GetAge() => Task.FromResult(this.unwrappedState.Age);
        public Task SetAge(int value) { this.unwrappedState.Age = value; return Task.CompletedTask; }
        #endregion IPersonInterface

        #region IJobInterface
        public Task<string> GetTitle() => Task.FromResult(this.unwrappedState.Title);
        public Task SetTitle(string value) { this.unwrappedState.Title = value; return Task.CompletedTask; }

        public Task<string> GetDepartment() => Task.FromResult(this.unwrappedState.Department);
        public Task SetDepartment(string value) { this.unwrappedState.Department = value; return Task.CompletedTask; }
        #endregion IJobInterface

        #region IEmployeeProperties
        public Task<int> GetEmployeeId() => Task.FromResult(this.unwrappedState.EmployeeId);
        public Task SetEmployeeId(int value) { this.unwrappedState.EmployeeId = value; return Task.CompletedTask; }
        #endregion IEmployeeProperties

        #region IEmployeeGrainState - not indexed
        public Task<int> GetSalary() => Task.FromResult(this.unwrappedState.Salary);
        public Task SetSalary(int value) { this.unwrappedState.Salary = value; return Task.CompletedTask; }
        #endregion IEmployeeGrainState - not indexed

        public Task WriteState() => this.WriteStateAsync();

        public Task Deactivate() { base.DeactivateOnIdle(); return Task.CompletedTask; }

        protected TestEmployeeGrain(IIndexWriter<TGrainState> indexWriter) => this.indexWriter = indexWriter;

        #region Facet methods - required overrides of Grain<TGrainState>
        public override Task OnActivateAsync() => this.indexWriter.OnActivateAsync(this, base.State, base.WriteStateAsync, base.OnActivateAsync);
        public override Task OnDeactivateAsync() => this.indexWriter.OnDeactivateAsync(() => Task.CompletedTask);
        protected override Task WriteStateAsync() => this.indexWriter.WriteAsync();
        #endregion Facet methods - required overrides of Grain<TGrainState>

        #region Required shims for IIndexableGrain methods for fault tolerance
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexWriter.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexWriter.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion Required shims for IIndexableGrain methods for fault tolerance
    }
}
