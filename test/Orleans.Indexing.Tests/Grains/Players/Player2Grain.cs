using Orleans.Indexing.Facet;
using Orleans.Providers;

namespace Orleans.Indexing.Tests
{
#if ALLOW_FT_ACTIVE
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = IndexingTestConstants.GrainStore)]
    public class Player2Grain : PlayerGrainFaultTolerant<PlayerGrainState>, IPlayer2Grain
    {
        public Player2Grain(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
#endif // ALLOW_FT_ACTIVE
}
