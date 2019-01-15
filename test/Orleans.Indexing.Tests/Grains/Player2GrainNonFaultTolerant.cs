using System;
using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player2GrainNonFaultTolerant : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayer2GrainNonFaultTolerant
    {
        public Player2GrainNonFaultTolerant(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<PlayerGrainState> indexWriter)
            : base(indexWriter) { }
    }
}
