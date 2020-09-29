﻿using Discord;
using DiscordBot.Core.Models;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace DiscordBot.Escrow
{
    public class BetView
    {
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

        private static string Bettors(Bet bet)
        {
            if (bet == null || bet.Bettors.Count() == 0)
                return "none";

            var groups = bet.Bettors
                .GroupBy(b => b.BetOptionId)
                .Select(g => 
                {
                    string optionName = BetOptionNameById(bet, g.Key);
                    IEnumerable<string> bettors = g
                     .OrderByDescending(b => b.Amount)
                     .Select(b => $"{MentionUtils.MentionUser(b.UserId)} bet {b.Amount} Attarcoins.");
                    return string.Format("{0}:\n\n{1}", optionName, string.Join("\n", bettors));
                });

            return string.Join("\n\n", groups);
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

        public static Embed BetOptionCreated(Bet bet, BetOption option, IUser author)
        {
            return new EmbedBuilder()
                 .WithAuthor(author)
                 .WithTitle("Bet option added")
                 .WithDescription($"Bet option '{option.Name}' added to bet '{bet.Name}'.")
                 .AddField("Place bet", $"$bet-place \"{bet.Name}\" {option.Id} AMOUNT")
                 .AddField("Bettors", Bettors(bet))
                 .WithColor(Color.Green)
                 .Build();
        }

        public static Embed BetPlaced(Bet bet, Bettor bettor, IUser author)
        {
            return new EmbedBuilder()
                .WithAuthor(author)
                .WithTitle($"Bet placed")
                .WithDescription($"{MentionUtils.MentionUser(bettor.UserId)} placed new bet on '{bet.Name}' option '{BetOptionNameById(bet, bettor.BetOptionId)}' for {bettor.Amount} Attarcoins.")
                .AddField("Withdraw or release bet", $"$bet-release \"{bet.Name}\"")
                .AddField("Bettors", Bettors(bet))
                .WithColor(Color.Green)
                .Build();
        }

        public static Embed BetResolved(Bet bet, IEnumerable<BetReward> rewards, Optional<IUser> author)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle($"Bet '{bet.Name}' resolved")
                .WithDescription(string.IsNullOrWhiteSpace(bet.Desc) ? "No description." : bet.Desc)
                .AddField("Bettors", Bettors(bet))
                .AddField("Rewards", Rewards(rewards))
                .WithColor(Color.Green);

            if (author.IsSpecified)
                builder.WithAuthor(author.Value);

            return builder.Build();
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
                .AddField("Create quick bet with default odds 2.00", "$bet-quick AMOUNT \n$bet-create \"BET NAME\" \"option one, option two, option three\"")
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
