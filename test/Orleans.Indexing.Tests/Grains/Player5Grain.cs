using System;
using Orleans.Providers;

namespace Orleans.Indexing.Tests
{
    [Serializable]
    public class Player5GrainState : Player5Properties, IPlayerState
    {
        public string Email { get; set; }
    }

    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = "GrainStore")]
    public class Player5Grain : PlayerGrain<Player5GrainState, Player5Properties>, IPlayer5Grain
    {
    }
}
