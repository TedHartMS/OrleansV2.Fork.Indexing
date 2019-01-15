using Orleans.Indexing.Facet;
using Orleans.Providers;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = IndexingTestConstants.GrainStore)]
    public class Player5Grain : PlayerGrain<PlayerGrainState>, IPlayer5Grain
    {
        public Player5Grain(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
}
