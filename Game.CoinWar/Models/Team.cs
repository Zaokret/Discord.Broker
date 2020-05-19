using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class Team
    {
        public Team(IGrouping<int, Player> grouping)
        {
            CurrentBet = grouping.Aggregate(0, (teamBetAmount, player) => teamBetAmount + player.CurrentBet);
            CoinsLeft = grouping.Aggregate(0, (coinsTotal, player) => coinsTotal + player.Coins);
            Players = grouping.Select(p => p);
            TeamId = grouping.Key;
        }
        public int CoinsLeft { get; set; }
        public int CurrentBet { get; set; }
        public int TeamId { get; set; }
        public IEnumerable<Player> Players { get; set; }
    }
}
