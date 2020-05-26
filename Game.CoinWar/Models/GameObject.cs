using Discord;
using DiscordBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public static class GameConfiguration
    {
        public static readonly int NumberOfRounds = 9;
        public static readonly int GameCoins = 100;
        public static readonly float BuyIn = 1f;
        public static readonly int RoundsToVictory = (int)decimal.Ceiling(new decimal(NumberOfRounds) / 2);
        public static readonly int MinimumBet = 5;
        public static readonly int SecondsBeforeStart = 15;
    }
    
    public class Score
    {
        public int WinnerTeamId { get; set; }
        public WinnerRule Rule { get; set; }
    }

    public enum WinnerRule
    {
        TrueTie,
        MoreCoinsLeft,
        MoreRoundsWon
    }
    
    public class GameObject
    {
        public GameObject(PendingGame pendingGame)
        {
            Guid = pendingGame.Guid;
            WinnerFound = false;
            Rounds = new List<Round>();
            Players = new List<Player>(new[]
            {
                new Player() { User = pendingGame.Initiator, Coins = GameConfiguration.GameCoins, CurrentBet = 0, TeamId = 1, Winner = false },
                new Player() { User = pendingGame.Challanged, Coins = GameConfiguration.GameCoins, CurrentBet = 0, TeamId = 2, Winner = false }
            });

            Collectable = pendingGame.Collectable;
            BuyInInvestment = GameConfiguration.BuyIn;
            GameCoins = GameConfiguration.GameCoins;
            NumberOfRounds = GameConfiguration.NumberOfRounds;
            RoundsToReward = GameConfiguration.RoundsToVictory;
            MinimumBet = GameConfiguration.MinimumBet;
        }
        public int RoundsToReward { get; set; }
        public string Guid { get; set; }
        public int NumberOfRounds { get; set; }
        public ICollection<Round> Rounds { get; set; }
        public float BuyInInvestment { get; set; }
        public int GameCoins { get; set; }
        public ICollection<Player> Players { get; set; }
        public bool WinnerFound { get; set; }
        public CollectableEntity Collectable { get; set; }
        public int MinimumBet { get; set; }

        public void EndWithWinner(int teamId)
        {
            WinnerFound = true;
            Players = Players
                .Select(p => p.Win(teamId))
                .ToList();
            
        }

        public Score Score()
        {
            var scores = Rounds
                  .Where(r => !r.BothTeamsLostRewards)
                  .GroupBy(r => r.WinnerTeamId)
                  .Select(g =>
                    new GameScore(
                        g.Key,
                        g.Count(),
                        Players.FirstOrDefault(p => p.TeamId == g.Key).CoinsLeft()))
                  .OrderByDescending(s => s.RoundsWon).ToList();
            
            if(scores.Count == 1)
            {
                return scores.Select(s => new Score { WinnerTeamId = s.TeamId, Rule = WinnerRule.MoreRoundsWon }).FirstOrDefault();
            }

            if (scores.Select(s => s.RoundsWon).Distinct().Count() != 1)
            {
                return new Score
                {
                    WinnerTeamId = scores.FirstOrDefault().TeamId,
                    Rule = WinnerRule.MoreRoundsWon
                };
            }
            else if (scores.Select(s => s.CoinsLeft).Distinct().Count() != 1)
            {
                return new Score
                {
                    WinnerTeamId = scores.FirstOrDefault().TeamId,
                    Rule = WinnerRule.MoreCoinsLeft
                };
            }
            else
            {
                return new Score
                {
                    WinnerTeamId = 0,
                    Rule = WinnerRule.TrueTie
                };
            }
            
        }

        public (Player, Player) PlayersByTeamId(int teamId)
        {
            Player playerInTeam = Players.FirstOrDefault(p => p.TeamId == teamId);
            Player playerInAnotherTeam = Players.FirstOrDefault(p => p.TeamId != teamId);
            return (playerInTeam, playerInAnotherTeam);
        }
    }

}
