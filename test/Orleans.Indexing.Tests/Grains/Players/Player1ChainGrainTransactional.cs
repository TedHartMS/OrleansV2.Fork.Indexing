using Orleans.Indexing.Facet;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player1ChainGrainTransactional : PlayerGrainTransactional<PlayerGrainState>, IPlayerChain1GrainTransactional
    {
        public Player1ChainGrainTransactional(
            [TransactionalIndexedState(IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState,
            [TransactionalState(IndexingConstants.BucketStateName, IndexingConstants.INDEXING_STORAGE_PROVIDER_NAME)]
            ITransactionalState<IndexedGrainStateWrapper<PlayerGrainState>> transactionalState)
            : base(indexedState, transactionalState) { }
    }
}
