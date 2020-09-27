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

        private static string Encode(string s) => s.Replace("(", "%28").Replace(")", "%29");

        private static string Link(string name, string url) =>
            string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : $"[{name}]({Encode(url)})";

        public static Embed TasteItem(TasteItem item)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle($"{item.Name} ({item.Type})")
                .WithDescription($"{(string.IsNullOrWhiteSpace(item.WTeaser) ? string.Empty : item.WTeaser)}\n\n{Link("Wikipedia", item.WUrl)}\n{Link("YouTube", item.YUrl)}")
                .WithColor(Color.LightOrange);

            return builder.Build();
        }

        public static Embed Commands(IUser user, string payPalUrl)
        {
            string[] coins = new string[]
            {
                "$coins - check your balance",
                "$coins @user - check user's balance",
                "$transfer X @user - transfer X number of coins to user",
                "$rich - check 5 richest users and your rank",
                "$flip - flip a coin"
            };

            string[] polls = new string[]
            {
                "$poll \"optionName1, optionName2, optionName3\" Title: \"Title Text\" Description: \"Description Text\" - vote between options",
                "$poll \"\" Title: \"Title Text\" Description: \"Description Text\" - vote yes/no"
            };

            string auction = "$auction @user - challange user or accept user's challange to an auction game";

            string[] occult = new[]
            {
                "$occult - create game lobby",
                "$join - join game lobby",
                "$leave - leave game lobby",
                "$role @user - vote user's role in the game",
                "$excommunicate @user - everyone's in game command: vote to remove suspected cultist from the game",
                "$sacrifice @user - cultist's in game command: vote to remove one of moderates from the game"
            };

            string help = "$help - provides you with first-grade relaxation audiotape";

            string donateDesc = $"Keep AttarcoinBroker bot running by [donating]({payPalUrl}).";

            string donateCommnad = "$donate - provides you with donate link.";

            string[] media = new string[]
            {
                "$media \"search term\" - info on music, movies, shows, podcasts, books, authors, games.",
                "$recommend \"search term\" - media recommendations based on searched term."
            };

            string bets = "$bet-help - bet instructions and commands";

            return new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("Commands")
                .WithDescription(donateDesc)
                .AddField("Coins", string.Join("\n", coins))
                .AddField("Auction", auction)
                .AddField("Bets", bets)
                .AddField("Occult", string.Join("\n", occult))
                .AddField("Poll", string.Join("\n", polls))
                .AddField("Help", help)
                .AddField("Donate", donateCommnad)
                .AddField("Media", string.Join("\n", media))
                .WithColor(Color.Green)
                .Build();
        }
    }
}
