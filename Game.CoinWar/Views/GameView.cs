using Discord;
using DiscordBot.Game.CoinWar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar.Views
{
    public class GameView
    {
        public static Embed Info(PendingGame game)
        {
            IEnumerable<string> description = new[] {
                $"Attar has asked the broker to sell all of his {GameConfiguration.NumberOfRounds} {game.Collectable.ItemName}s. If he succeeds he'll get a bonus.",
                "Broker invites interested collectors to join free of charge.",
                $"You the {game.Collectable.Title}, who is missing {GameConfiguration.RoundsToVictory} {game.Collectable.ItemName}s from their most prized collection attend the auction with a budget of {GameConfiguration.GameCoins} coins.",
                $"Broker will auction {game.Collectable.ItemName}s one by one hoping to earn the bonus.",
            };

            IEnumerable<string> rules = new[]
            {
                $"You have {GameConfiguration.GameCoins} coins to spend.",
                $"Purchase {GameConfiguration.RoundsToVictory} {game.Collectable.ItemName}s and complete your collection.",
                "Each round make a better offer to secure the purchase.",
                $"Minimum offer is {GameConfiguration.MinimumBet} coins.",
                "You're unaware of other buyer offerings.",
                "Buyer is determined after the first offer.",
                "In case you offer the same amount as the other buyer, you are asked increase an offer by an amount until a buyer is determined."
            }.Select(s => $" -  {s}");

            string reward = "If you complete your collection you keep the coins you didn't spend.";

            return new EmbedBuilder()
                .WithTitle($"{game.Collectable.EmoteName}   A U C T I O N    G A M E   {game.Collectable.EmoteName}")
                .WithDescription(string.Join("\n\n", description))
                .AddField("# Rules", string.Join("\n", rules))
                .AddField("# Reward", reward)
                .WithColor(Color.Gold)
                .Build();
        }
    }
}
