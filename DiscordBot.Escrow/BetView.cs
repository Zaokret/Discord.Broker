using Discord;
using DiscordBot.Core.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace DiscordBot.Escrow
{
    public class BetView
    {
        public static Embed MineBets(IEnumerable<Bet> bets, ulong userId)
        {
            if (!bets.Any())
                return BetHelp();

            IEnumerable<EmbedFieldBuilder> fields = bets.Select(bet =>
            {
                List<string> options = bet.Options.Select(option =>
                {
                    int betted = bet.Bettors
                    .Where(b => b.BetOptionId == option.Id)
                    .Aggregate(0, (total, b) => total + b.Amount);
                    return $"[{option.Id}] ({option.Odds:F}) {option.Name} with the weight of {betted} coins.";
                }).ToList();

                Bettor bettor = bet.Bettors.FirstOrDefault(b => b.UserId == userId);
                BetOption bettorsOption = bet.Options.FirstOrDefault(o => o.Id == bettor.BetOptionId);
                options.Add($"\nYou placed {bettor.Amount} Attarcoin on option \"{bettorsOption.Name}\". {(bettor.Released ? "(Released)" : "")}");

                return new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName($"{bet.Name} ({(bet.Resolved ? "Inactive" : "Active")})")
                .WithValue(options.Count == 0 ? "No options added yet." : string.Join("\n", options));
            });

            return new EmbedBuilder()
                .WithTitle("Your bets")
                .WithColor(Color.Green)
                .WithFields(fields)
                .Build();
        }

        private static string Options(Bet bet)
        {
            if (bet == null || bet.Options.Count() == 0)
                return string.Empty;
            return string.Join("\n", bet.Options.Select(o => $"[{o.Id}] ({o.Odds:F}) {o.Name}"));
        }

        private static string BetOptionNameById(Bet bet, int betOptionId)
        {
            BetOption option = bet.Options.FirstOrDefault(o => o.Id == betOptionId);
            return option?.Name ?? "unknown";
        }

        private static IEnumerable<EmbedBuilder> BettorEmbeds(Bet bet)
        {
            if (bet == null || bet.Bettors.Count() == 0)
                return new List<EmbedBuilder>();

            var groups = bet.Bettors
                .GroupBy(b => b.BetOptionId)
                .Select(g => 
                {
                    BetOption option = bet.Options.FirstOrDefault(o => o.Id == g.Key);

                    IEnumerable<string> bettors = g
                     .OrderByDescending(b => b.Amount)
                     .Select(b => $"{MentionUtils.MentionUser(b.UserId)} bet {b.Amount} Attarcoins.");

                    return new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle($"Option \"{option.Name}\" on bet \"{bet.Name}\"")
                    .WithDescription($"Id: [{option.Id}]\nOdds: ({option.Odds:F})\n\n{string.Join("\n", bettors)}");
                });

            return groups;
        }

        private static EmbedFieldBuilder BettorDescription(Bet bet)
        {
            var builder = new EmbedFieldBuilder()
                .WithName("Bettors")
                .WithIsInline(false);

            if (bet == null || bet.Bettors.Count() == 0)
                return builder.WithValue("none");

            var groups = bet.Bettors
                .GroupBy(b => b.BetOptionId)
                .Select(g =>
                {
                    BetOption option = bet.Options.FirstOrDefault(o => o.Id == g.Key);
                    IEnumerable<string> bettors = g
                     .OrderByDescending(b => b.Amount)
                     .Select(b => $"{MentionUtils.MentionUser(b.UserId)} bet {b.Amount} Attarcoins.");

                    return $"[{option.Id}] ({option.Odds:F}) {option.Name}\n\n{string.Join("\n", bettors)}";
                });

            return builder.WithValue(string.Join("\n\n", groups));
        }

        private static string Rewards(IEnumerable<BetReward> rewards)
        {
            if (rewards.Count() == 0)
                return "none";
            return string.Join("\n", rewards.Select(s => $"{MentionUtils.MentionUser(s.UserId)} wins {s.Amount} Attarcoins."));
        }

        public static Embed Bets(IEnumerable<Bet> bets, string description)
        {
            if (!bets.Any())
                return BetHelp();

            IEnumerable<EmbedFieldBuilder> fields = bets.Select(bet =>
            {
                List<string> options = bet.Options.Select(option =>
                {
                    int betted = bet.Bettors
                    .Where(bettor => bettor.BetOptionId == option.Id)
                    .Aggregate(0, (total, bettor) => total + bettor.Amount);
                    return $"[{option.Id}] ({option.Odds:F}) {option.Name} with the weight of {betted} coins.";
                }).ToList();

                return new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName($"{bet.Name} ({(bet.Resolved ? "Inactive" : "Active")})")
                .WithValue(options.Count == 0 ? "No options added yet." : string.Join("\n", options));
            });
            
            return new EmbedBuilder()
                .WithTitle($"{description} bets")
                .WithColor(Color.Green)
                .WithFields(fields)
                .Build();
        }

        public static Embed BetCreated(Bet bet, IUser author)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithTitle($"Bet '{bet.Name}' created")
                .AddField("Add bet option", $"$bet-option \"{bet.Name}\" \"OPTION NAME\" ODDS")
                .WithColor(Color.Green);

            if (!string.IsNullOrWhiteSpace(bet.Desc)) 
                builder.WithDescription(bet.Desc);

            string options = Options(bet);
            if (!string.IsNullOrWhiteSpace(options))
                builder.AddField("Options", options);

            return builder.Build();
        }

        public static Embed QuickBetCreated(Bet bet, IUser author, int amount)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle($"Quick bet created")
                .WithDescription($"With two options with the odds of 2. {author.Mention} placed {amount} on the first option.")
                .AddField($"Bet against {author.Username}", $"$bet-place \"{bet.Name}\" 2 {amount}");

            if (!string.IsNullOrWhiteSpace(bet.Desc))
                builder.WithDescription(bet.Desc);

            return builder.Build();
        }

        private static IEnumerable<Embed> WithBettors(Bet bet, EmbedBuilder builder)
        {
            List<EmbedBuilder> embeds = new List<EmbedBuilder>();
            var bettors = BettorDescription(bet);

            embeds.Add(builder);

            if (bettors.Value.ToString().Length <= EmbedFieldBuilder.MaxFieldValueLength)
            {
                builder.AddField(bettors);
            }
            else
            {
                embeds.AddRange(BettorEmbeds(bet));
            }

            return embeds.Select(e => e.Build());
        }

        public static IEnumerable<Embed> BetOptionCreated(Bet bet, BetOption option, IUser author)
        {
            var builder = new EmbedBuilder()
                 .WithAuthor(author)
                 .WithTitle("Bet option added")
                 .WithDescription($"Bet option '{option.Name}' added to bet '{bet.Name}'.")
                 .AddField("Place bet", $"$bet-place \"{bet.Name}\" {option.Id} AMOUNT")
                 .WithColor(Color.Green);
            return WithBettors(bet, builder);
        }

        public static IEnumerable<Embed> BetPlaced(Bet bet, Bettor bettor, IUser author)
        {
            var builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithTitle($"Bet placed")
                .WithDescription($"{MentionUtils.MentionUser(bettor.UserId)} placed new bet on '{bet.Name}' option '{BetOptionNameById(bet, bettor.BetOptionId)}' for {bettor.Amount} Attarcoins.")
                .AddField("Withdraw or release bet", $"$bet-release \"{bet.Name}\"")
                .WithColor(Color.Green);
            return WithBettors(bet, builder);
        }

        public static IEnumerable<Embed> BetResolved(Bet bet, IEnumerable<BetReward> rewards, Optional<IUser> author)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle($"Bet '{bet.Name}' resolved")
                .WithDescription(string.IsNullOrWhiteSpace(bet.Desc) ? "No description." : bet.Desc)
                .AddField("Bettors", BettorDescription(bet))
                .AddField("Rewards", Rewards(rewards))
                .WithColor(Color.Green);

            if (author.IsSpecified)
                builder.WithAuthor(author.Value);

            return WithBettors(bet, builder);
        }

        public static Embed BetHelp()
        {
            string[] instructions = new string[]
            {
                "AttarcoinBroker offers secure escrow services. Create bets, add bet options with custom odds and place bets.",
                "You may bet in many different bets. " +
                "You may place your bet on only one of the bet options within a single bet. " +
                "You may at any time while the bet is active add more coins to your bet. " +
                "You may withdraw your bet while a bet has one bet option or you are the only bettor.",
                "Once the bet result is known and all bettors that lost have issued a command to admit their loss, " +
                "rewards will be given to the winners: bet option odds x bet amount. If losers don't do it in a timely manner, " +
                "bet will be taken to court and judge will resolve the bet.",
            };

            return new EmbedBuilder()
                .WithTitle("Bet instructions")
                .WithDescription(string.Join("\n\n", instructions))
                .AddField("Create custom bet", "$bet-create \"BET NAME\" \"BET DESCRIPTION\" \n$bet-create \"BET NAME\"")
                .AddField("Create quick bet with default odds 2.00", "$bet-quick AMOUNT \n$bet-quick \"BET NAME\" \"option one, option two, option three\"")
                .AddField("Create bet option", "$bet-option \"BET NAME\" \"OPTION NAME\" ODDS")
                .AddField("Place bet or add more coins to your bet", "$bet-place \"BET NAME\" OPTIONID AMOUNT")
                .AddField("Admit loss", "$bet-release \"BET NAME\"")
                .AddField("Call the courts", "$bet-judge")
                .AddField("Withdraw bet", "$bet-release \"BET NAME\"")
                .AddField("Judge command only", "$bet-resolve \"BET NAME\" OPTIONID")
                .AddField("Get bet by name", "$bet-find \"BET NAME\"")
                .AddField("Get your bets", "$bet-mine")
                .AddField("Get top bets", "$bet-top")
                .AddField("Get user's bets", "$bet-user @user")
                .AddField("Get active bets", "$bet-active")
                .AddField("Get inactive bets", "$bet-inactive")
                .AddField("Flip a coin", "$flip")
                .WithColor(Color.Green)
                .Build();
        }
    }
}
