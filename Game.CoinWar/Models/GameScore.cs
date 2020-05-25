using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class GameScore
    {
        public GameScore(int teamId, int roundsWon, int coinsLeft)
        {
            RoundsWon = roundsWon;
            TeamId = teamId;
            CoinsLeft = coinsLeft;
        }
        public int RoundsWon { get; set; }
        public int TeamId { get; set; }
        public int CoinsLeft { get; set; }
    }
}
