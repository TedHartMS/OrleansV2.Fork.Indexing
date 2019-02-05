using Orleans.Indexing.Facet;
using Orleans.Providers;

namespace Orleans.Indexing.Tests
{
#if ALLOW_FT_ACTIVE
    /// <summary>
    /// A simple grain that represents a player in a game
    /// </summary>
    [StorageProvider(ProviderName = IndexingTestConstants.GrainStore)]
    public class Player1Grain : PlayerGrainFaultTolerant<PlayerGrainState>, IPlayer1Grain
    {
        public Player1Grain(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
#endif // ALLOW_FT_ACTIVE
}
