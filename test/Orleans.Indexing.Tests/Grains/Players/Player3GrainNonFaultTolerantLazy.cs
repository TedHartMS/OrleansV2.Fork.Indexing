using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player3GrainNonFaultTolerantLazy : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayer3GrainNonFaultTolerantLazy
    {
        public Player3GrainNonFaultTolerantLazy(
            [NonFaultTolerantWorkflowIndexedState]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
