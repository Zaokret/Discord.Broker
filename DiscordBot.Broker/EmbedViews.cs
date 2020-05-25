using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Broker
{
    public class EmbedViews
    {
        private static string UserRankDescription(RankedUser u) {
            string name = u?.User?.Username ?? "Unknown";
            return $"{u.Rank}. {name} ({u.Points})";
        }

        public static Embed Leaderboard(LeaderboardView leaderboard)
        {
            string description = string.Join("\n\n", leaderboard.TopUsers.Select(u => UserRankDescription(u)));

            return new EmbedBuilder()
                .WithTitle("UPPER CRUST ATTARIANS")
                .WithDescription(description)
                .AddField("You", UserRankDescription(leaderboard.IssuerRanking))
                .WithColor(Color.Gold)
                .Build();
        }
    }
}
