namespace Orleans.Indexing.Tests
{
    public class Player1Properties : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex]
        public string Location { get; set; }
    }

    public interface IPlayer1Grain : IPlayerGrain, IIndexableGrain<Player1Properties>
    {
    }
}
