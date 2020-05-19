using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class GameObject
    {
        public GameObject(PendingGame game, IUser playerOne, IUser playerTwo)
        {
            Guid = game.Guid;
            NumberOfRounds = 9;
            RoundsToVictory = (int)decimal.Ceiling(new decimal(NumberOfRounds) / 2);
            BuyInInvestment = 1f;
            GameCoins = 100;
            Rounds = new List<Round>();
            Players = new List<Player>(new[]
            {
        new Player() { User = playerOne, Coins = GameCoins, CurrentBet = 0, TeamId = 1, Winner = false },
        new Player() { User = playerTwo, Coins = GameCoins, CurrentBet = 0, TeamId = 2, Winner = false }
      });
            WinnerFound = false;
        }
        public int RoundsToVictory { get; set; }
        public string Guid { get; set; }
        public int NumberOfRounds { get; set; }
        public ICollection<Round> Rounds { get; set; }
        public float BuyInInvestment { get; set; }
        public int GameCoins { get; set; }
        public ICollection<Player> Players { get; set; }
        public bool WinnerFound { get; set; }

        public void EndWithWinner(int teamId)
        {
            Players = Players
                .Select(p => p.Win(teamId))
                .ToList();
            WinnerFound = true;
        }
    }

}
