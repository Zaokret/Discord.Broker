using Discord;
using DiscordBot.Game.CoinWar.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Views
{
    public class RoundView
    {
        public static Func<Player, Embed> Of(int round, Player winner, Player loser)
        {
            return (player) => new EmbedBuilder()
                .WithTitle($"Round {round} breakdown")
                .AddField("Winner", RoundPlayerMessage(winner.User.Username, winner.CurrentBet), true)
                .AddField("Loser", RoundPlayerMessage(loser.User.Username, loser.CurrentBet), true)
                .WithColor(player.TeamId == winner.TeamId ? Color.Gold : Color.DarkRed)
                .Build();
        }

        private static string RoundPlayerMessage(string username, int currentBet) => $"{username} with {currentBet} coins.";
    }

    public class GameView
    {
        public static Func<Player, Embed> Of(GameObject game)
        {
            return (player) => new EmbedBuilder()
                .WithTitle($"COIN WAR")
                .WithDescription($"Game to spend your hard earned coins.")
                .AddField("Win condition", "Win 5 out of 9 rounds.")
                .AddField("Round", "Place a bet from starting stash of 100 coins. ")
                .AddField("Round win condition", "Higher bet wins.")
                .AddField("Round win condition", "Win 5 out of 9 rounds.")
                .WithColor(Color.Gold)
                .WithAuthor("AttarcoinBroker")
                .Build();
        }
    }
}
