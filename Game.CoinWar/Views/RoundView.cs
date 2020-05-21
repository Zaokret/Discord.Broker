using Discord;
using DiscordBot.Game.CoinWar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar.Views
{
    public class GameScoreView
    {
        private static Func<Round,int, string> RoundDescription(Player player)
        {
            return (Round round, int roundIndex) => !round.BothTeamLost && player.TeamId == round.WinnerTeamId
                ? $"({round.Winner.CurrentBet}) WON"
                : $"({round.Loser.CurrentBet}) LOST";
        }

        private static string GetRoundsDescription(IEnumerable<Round> rounds, Player player)
        {
            IEnumerable<string> roundDescriptions = rounds.Select(RoundDescription(player));
            return string.Join("\n", roundDescriptions);
        }

        private static string GetPlayerItems(List<Round> rounds, Player player)
        {
            string playerItems = string.Join(" ", Enumerable.Range(0, rounds.Count(r => r.WinnerTeamId == player.TeamId)).Select(s => "🐅"));
            return string.IsNullOrWhiteSpace(playerItems)
                ? "Empty cage."
                : playerItems;
        }

        private static string GetPlayerScoreDescription(List<Round> rounds, Player player)
        {
            return string.Join("\n\n", new[]
            {
                GetPlayerItems(rounds, player),
                GetRoundsDescription(rounds, player)
            });
        }

        public static Embed Of(GameObject game, Player playerOne, Player playerTwo)
        {
            List<Round> roundList = game.Rounds.ToList();
            Round lastRound = roundList.Last();

            string itemsLeft = string.Join(" ", Enumerable.Range(1, game.NumberOfRounds - roundList.Count).Select(s => "🐅"));
            if (string.IsNullOrWhiteSpace(itemsLeft))
            {
                itemsLeft = "No tigers left.";
            }

            return new EmbedBuilder()
                .WithTitle("Score board")
                .WithDescription(itemsLeft)
                .AddField($"Winner of round # {roundList.Count}", lastRound.Winner.User.Username)
                .AddField($"{playerOne.User.Username} ({playerOne.Coins})", GetPlayerScoreDescription(roundList, playerOne), true)
                .AddField($"{playerTwo.User.Username} ({playerTwo.Coins})", GetPlayerScoreDescription(roundList, playerTwo), true)
                .WithColor(Color.LightOrange)
                .Build();
        }
    }

    public class GameView
    {
        public static Embed Info(GameObject game)
        {
            IEnumerable<string> description = new[] {
                $"Attar has asked the broker to sell all of his {game.NumberOfRounds} tigers. If he succeeds he'll get a bonus.",
                "Broker invites interested collectors to join free of charge.",
                $"You the collector, who is missing {game.RoundsToVictory} tigers from their most prized collection attend the auction with a budget of {game.GameCoins} coins.",
                "Broker will auction tigers one by one hoping to earn the bonus.",
            };

            IEnumerable<string> rules = new[]
            {
                $"You have {game.GameCoins} coins to spend.",
                $"Purchase {game.RoundsToVictory} tigers and complete your collection.",
                "Each round make a better offer to seal the purchase.",
                "You're unaware of other buyer offerings.",
                "Buyer is determined after the first offer.",
                "In case you offer the same amount as the other buyer, you are asked increase an offer by an amount until a buyer is determined."
            }.Select(s => $" -  {s}");

            string reward = "If you complete your collection you keep the coins you didn't spend.";

            return new EmbedBuilder()
                .WithTitle($"🐯   T I G E R    K I N G   🐯")
                .WithDescription(string.Join("\n\n", description))
                .AddField("# Rules", string.Join("\n", rules))
                .AddField("# Reward", reward)
                .WithColor(Color.Gold)
                .Build();
        }
    }
}
