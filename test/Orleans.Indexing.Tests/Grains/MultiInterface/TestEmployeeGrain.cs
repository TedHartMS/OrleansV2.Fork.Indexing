using Orleans.Providers;
using System.Threading.Tasks;
using Orleans.Indexing.Facets;
using System;

namespace Orleans.Indexing.Tests.MultiInterface
{
    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public abstract class TestEmployeeGrain<TGrainState, TIPersonProps, TIJobProps> : Grain<TGrainState>
        where TGrainState : IEmployeeGrainState, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces
        // on this class.
        private IIndexWriter<TGrainState> indexWriter;

        // This illustrates implementing the Grain interfaces to get and set the properties.
        #region IPersonInterface
        public Task<string> GetLocation() => Task.FromResult(this.State.Location);
        public Task SetLocation(string value) { this.State.Location = value; return Task.CompletedTask; }

        public Task<int> GetAge() => Task.FromResult(this.State.Age);
        public Task SetAge(int value) { this.State.Age = value; return Task.CompletedTask; }
        #endregion IPersonInterface

        #region IJobInterface
        public Task<string> GetTitle() => Task.FromResult(this.State.Title);
        public Task SetTitle(string value) { this.State.Title = value; return Task.CompletedTask; }

        public Task<string> GetDepartment() => Task.FromResult(this.State.Department);
        public Task SetDepartment(string value) { this.State.Department = value; return Task.CompletedTask; }
        #endregion IJobInterface

        #region IEmployeeState
        public Task<int> GetTenure() => Task.FromResult(this.State.Tenure);
        public Task SetTenure(int value) { this.State.Tenure = value; return Task.CompletedTask; }
        #endregion IEmployeeState

        public Task Deactivate()
        {
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        protected TestEmployeeGrain(
            IIndexWriter<TGrainState> indexWriter)
        {
            this.indexWriter = indexWriter;
        }

        protected override async Task WriteStateAsync()
        {
            await this.indexWriter.Write(this, () => base.WriteStateAsync());
        }


        // TODO remove these when facetization complete
        public Task<object> ExtractIndexImage(IIndexUpdateGenerator iUpdateGen) => throw new System.NotImplementedException();
        public Task<Concurrency.Immutable<System.Collections.Generic.HashSet<System.Guid>>> GetActiveWorkflowIdsList() => throw new System.NotImplementedException();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => throw new System.NotImplementedException();
    }
}
