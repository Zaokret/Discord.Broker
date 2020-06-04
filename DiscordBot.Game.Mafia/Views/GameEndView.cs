using Discord;
using DiscordBot.Game.Mafia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DiscordBot.Game.Mafia.MafiaService;

namespace DiscordBot.Game.Mafia.Views
{
    public static class GameEndView
    {
        private static string IsAlive(bool alive)
        {
            return alive ? "yes" : "no";
        }

        public static EmbedFieldBuilder PlayerScore(PlayerReward obj)
        {
            string description = string.Join("\n", new string[]
            {
                string.Empty,
                $"Role: {GameElement.Role(obj.Player.Role)}",
                $"Alive: {IsAlive(obj.Player.Active)}",
                $"Alive for: {obj.DaysAliveFor} days",
                $"Reward: {obj.Reward} coins"
            });
            return new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName(obj.Player.User.Username)
                .WithValue(description);
        }

        public static Embed Of(GroupType group, List<PlayerReward> rewardedPlayers)
        {
            EmbedFieldBuilder[] playerScore = rewardedPlayers
                .OrderByDescending(s => s.Reward)
                .Select(PlayerScore)
                .ToArray();

            return new EmbedBuilder()
                .WithTitle("S C O R E B O A R D")
                .WithDescription($"{GameElement.Group(group)} WIN")
                .WithFields(playerScore)
                .WithColor(Color.DarkRed)
                .Build();
        }

        public static string Message(GroupType group)
        {
            if (group == GroupType.Uninformed)
            {
                return $"{GameElement.Channel.Private()} is no more.";
            }
            else
            {
                return $"{GameElement.Channel.Private()} everlasting.";
            }
        }
    }
}
