using Orleans.Indexing.Facet;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player3GrainTransactional : PlayerGrainTransactional<PlayerGrainState>, IPlayer3GrainTransactional
    {
        public Player3GrainTransactional(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
