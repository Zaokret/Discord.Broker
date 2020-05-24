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
        private static Func<Round, int, string> RoundDescription(Player player)
        {
            return (Round round, int roundIndex) => !round.BothTeamsLostRewards && player.TeamId == round.WinnerTeamId
                ? $"({round.Winner.CurrentBet}) WON"
                : $"({round.Loser.CurrentBet}) LOST";
        }

        private static string GetRoundsDescription(IEnumerable<Round> rounds, Player player)
        {
            IEnumerable<string> roundDescriptions = rounds.Select(RoundDescription(player));
            return string.Join("\n", roundDescriptions);
        }

        private static string GetPlayerItems(List<Round> rounds, Player player, string emote)
        {
            string playerItems = string.Join(" ", Enumerable.Range(0, rounds.Count(r => r.WinnerTeamId == player.TeamId)).Select(s => emote));
            return string.IsNullOrWhiteSpace(playerItems)
                ? "Empty"
                : playerItems;
        }

        private static string GetPlayerScoreDescription(List<Round> rounds, Player player, string emote)
        {
            return string.Join("\n\n", new[]
            {
                GetPlayerItems(rounds, player, emote),
                GetRoundsDescription(rounds, player)
            });
        }

        public static Embed Of(GameObject game, Player playerOne, Player playerTwo, bool isFinalScoreBoard = false)
        {
            List<Round> roundList = game.Rounds.ToList();
            Round lastRound = roundList.Last();

            string itemsLeft = string.Join(" ", Enumerable.Range(1, game.NumberOfRounds - roundList.Count).Select(s => game.Collectable.EmoteName));
            if (string.IsNullOrWhiteSpace(itemsLeft))
            {
                itemsLeft = $"No {game.Collectable.ItemName}s left.";
            }

            return new EmbedBuilder()
                .WithTitle(isFinalScoreBoard ? "Final score board" : "Score board")
                .WithDescription(itemsLeft)
                .AddField($"Winner of round # {roundList.Count}", lastRound.Winner.User.Username)
                .AddField($"{playerOne.User.Username} ({playerOne.Coins})", GetPlayerScoreDescription(roundList, playerOne, game.Collectable.EmoteName), true)
                .AddField($"{playerTwo.User.Username} ({playerTwo.Coins})", GetPlayerScoreDescription(roundList, playerTwo, game.Collectable.EmoteName), true)
                .WithColor(Color.LightOrange)
                .Build();
        }
    }

}
