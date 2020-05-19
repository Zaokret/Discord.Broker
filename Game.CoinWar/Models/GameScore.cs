using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class GameScore
    {
        public GameScore(int teamId, int roundsWon)
        {
            RoundsWon = roundsWon;
            TeamId = teamId;
        }
        public int RoundsWon { get; set; }
        public int TeamId { get; set; }
    }
}
