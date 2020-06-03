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
        public static Embed Of(GroupType group, List<PlayerReward> rewardedPlayers)
        {
            string description = 
                string.Join("\n\n", rewardedPlayers.Select(r => $"{r.Player.User.Username} ({GameElement.Role(r.Player.Role)}) earned {r.Reward} coins."));

            return new EmbedBuilder()
                .WithTitle($"{GameElement.Group(group)} WIN")
                .WithDescription(description)
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
