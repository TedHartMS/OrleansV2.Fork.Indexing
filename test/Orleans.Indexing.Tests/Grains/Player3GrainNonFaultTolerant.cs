using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player3GrainNonFaultTolerant : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayer3GrainNonFaultTolerant
    {
        public Player3GrainNonFaultTolerant(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
}
