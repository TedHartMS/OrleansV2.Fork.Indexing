using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Indexing.Facet;
using Orleans.Providers;

namespace Orleans.Indexing.Tests
{
    public abstract class PlayerGrainNonFaultTolerant<TGrainState> : PlayerGrain<TGrainState, IndexableGrainStateWrapper<TGrainState>>
        where TGrainState : PlayerGrainState, new()
    {
        public PlayerGrainNonFaultTolerant(IIndexWriter<TGrainState> indexWriter) : base(indexWriter)
        {
            Debug.Assert(this.GetType().GetIndexScheme() == IndexScheme.NonFaultTolerantWorkflow);
        }
    }

    public abstract class PlayerGrainFaultTolerant<TGrainState> : PlayerGrain<TGrainState, FaultTolerantIndexableGrainStateWrapper<TGrainState>>
        where TGrainState : PlayerGrainState, new()
    {
        public PlayerGrainFaultTolerant(IIndexWriter<TGrainState> indexWriter) : base(indexWriter)
        {
            Debug.Assert(this.GetType().GetIndexScheme() == IndexScheme.FaultTolerantWorkflow);
        }
    }

    /// <summary>
    /// A simple grain that represents a player in a game
    /// </summary>
    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public abstract class PlayerGrain<TGrainState, TWrappedState> : Grain<TWrappedState>, IPlayerGrain
        where TGrainState : PlayerGrainState, new()
        where TWrappedState : IndexableGrainStateWrapper<TGrainState>, new()
    {
        // This is populated by Orleans.Indexing with the indexes from the implemented interfaces on this class.
        private readonly IIndexWriter<TGrainState> indexWriter;

        private TGrainState unwrappedState => base.State.UserState;

        public string Email => this.unwrappedState.Email;
        public string Location => this.unwrappedState.Location;
        public int Score => this.unwrappedState.Score;

        public Task<string> GetLocation() => Task.FromResult(this.Location);

        public async Task SetLocation(string value)
            => await IndexingTestUtils.SetPropertyAndWriteStateAsync(() => this.unwrappedState.Location = value, this.WriteStateAsync, this.ReadStateAsync, retry:true);

        public Task<int> GetScore() => Task.FromResult(this.Score);

        public Task SetScore(int score)
        {
            this.unwrappedState.Score = score;
            return base.WriteStateAsync();
        }

        public Task<string> GetEmail() => Task.FromResult(this.Email);

        public Task SetEmail(string email)
        {
            this.unwrappedState.Email = email;
            return base.WriteStateAsync();
        }

        public Task Deactivate()
        {
            this.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public PlayerGrain(IIndexWriter<TGrainState> indexWriter)
        {
            this.indexWriter = indexWriter;
        }

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
