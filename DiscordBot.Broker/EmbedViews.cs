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
            if (u == null)
                return "Fella is off the books";
            return $"{u.Rank}. {u.User?.Username ?? "Unknown"} ({u.Points})";
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

        public static Embed Donate(string url)
        {
            return new EmbedBuilder()
                .WithTitle("Donate")
                .WithUrl(url)
                .WithDescription("Keep me in business.")
                .WithColor(Color.Blue)
                .Build();
        }
    }
}
