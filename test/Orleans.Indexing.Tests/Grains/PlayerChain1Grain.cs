using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class PlayerChain1Grain : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayerChain1Grain
    {
        public PlayerChain1Grain(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
}
