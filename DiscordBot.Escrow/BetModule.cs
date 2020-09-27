using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Core.Attributes;
using DiscordBot.Core.Models;
using DiscordBot.Core.Utilities;
using DiscordBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Escrow
{
    public class BetModule : ModuleBase<SocketCommandContext>
    {
        private readonly BetService _betService;
        private readonly CoinService _coinService;
        private readonly DynamicConfigurationRepository _dynamicConfigRepository;

        public BetModule(BetService betService, CoinService coinService, DynamicConfigurationRepository dynamicConfigRepository)
        {
            _betService = betService ?? throw new ArgumentNullException(nameof(betService));
            _coinService = coinService ?? throw new ArgumentNullException(nameof(coinService));
            _dynamicConfigRepository = dynamicConfigRepository ?? throw new ArgumentNullException(nameof(dynamicConfigRepository));
        }

        // HELP

        [Command("bet-help")]
        [Summary("Bet commands instructions.")]
        public async Task BetHelp()
        {
            await ReplyAsync(string.Empty, false, BetView.BetHelp());
        }

        // READ

        [Command("bet-active")]
        [Summary("Get active bets.")]
        public async Task GetActive()
        {
            
            await ReplyAsync(string.Empty, false, BetView.Bets(await _betService.GetBets((bet) => !bet.Resolved), "Active"));
        }

        [Command("bet-inactive")]
        [Summary("Get incative bets.")]
        public async Task GetInactive()
        {
            await ReplyAsync(string.Empty, false, BetView.Bets(await _betService.GetBets((bet) => bet.Resolved), "Inactive"));
        }

        [Command("bet-mine")]
        [Summary("Get bets command issuer participates in.")]
        public async Task GetUsersBets()
        {
            await ReplyAsync(
                string.Empty, 
                false, 
                BetView.Bets(
                    await _betService.GetBets((bet => bet.Bettors.Any(bettor => bettor.UserId == Context.User.Id))),
                    "Your"));
        }

        [Command("bet-user")]
        [Summary("Gets bets user participates in.")]
        public async Task GetBetsByUser(IUser user)
        {
            await ReplyAsync(
                string.Empty, 
                false, 
                BetView.Bets(
                    await _betService.GetBets((bet => bet.Bettors.Any(bettor => bettor.UserId == user.Id))),
                    $"{MentionUtils.MentionUser(user.Id)}'s"));
        }

        [Command("bet-top")]
        [Summary("Get top bets.")]
        public async Task GetTopBets()
        {
            await ReplyAsync(string.Empty, false, BetView.Bets(await _betService.GetTopBets(), "Top"));
        }

        [Command("bet-find")]
        [Summary("Create bet")]
        public async Task GetBetByName(string betName)
        {
            Bet bet = await _betService.GetBetByName(betName);
            if (bet != null)
                await ReplyAsync(string.Empty, false, BetView.Bets(new[] { bet }, $"'{betName}'"));
            else
                await ReplyAsync($"Couldn't find bet by the name of '{betName}'.");
        }

        // CREATE

        [RequiredJudgeAssigned]
        [Command("bet-quick")]
        [Summary("Create quick bet.")]
        public async Task CreateBet(int amount)
        {
            if(amount > 0)
            {
                float funds = await _coinService.GetFundsByUserId(Context.User.Id);
                if (amount > funds)
                {
                    await ReplyAsync($"Not enough Attarcoins. Available: {funds} Attarcoins.");
                }
                else
                {
                    Bet bet = await _betService.CreateQuickBet(Context.User, amount);
                    if(bet != null)
                    {
                        await _coinService.RemoveFunds(Context.User.Id, amount);
                        await ReplyAsync(string.Empty, false, BetView.QuickBetCreated(bet, Context.User, amount));
                    }
                    else
                    {
                        await ReplyAsync("Couldn't create a quick bet.");
                    }
                    
                }
            }
        }

        [RequiredJudgeAssigned]
        [Command("bet-create")]
        [Summary("Create bet")]
        public async Task CreateBet(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                await ReplyAsync("Invalid name or description.");
            }
            else
            {
                Bet existing = await _betService.GetBetByName(name);
                if (existing == null)
                {
                    Bet bet = await _betService.CreateBet(description, name);
                    await ReplyAsync(string.Empty, false, BetView.BetCreated(bet, Context.User));
                }
                else
                {
                    await ReplyAsync($"Bet with the name of '{name}' already exists.");
                }
            }
        }

        [RequiredJudgeAssigned]
        [Command("bet-create")]
        [Summary("Create bet")]
        public async Task CreateBet(string name)
        {
            await CreateBet(name, string.Empty);
        }

        [RequiredJudgeAssigned]
        [Command("bet-option")]
        [Summary("Add bet option to a bet.")]
        public async Task AddBetOption(string betName, string name, float odds)
        {
            Bet bet = await _betService.GetBetByName(betName);
            if (bet != null) 
            {
                BetOption option = await _betService.AddBetOption(name, odds, betName);
                if (option != null)
                {
                    await ReplyAsync(string.Empty, false, BetView.BetOptionCreated(bet, option, Context.User));
                }
                else
                {
                    await ReplyAsync("Can't create that option.");
                }
            }
            else
            {
                await ReplyAsync($"Bet '{betName}' not found.");
            }
        }

        [RequiredJudgeAssigned]
        [Command("bet-place")]
        [Summary("Place an amount on a bet option.")]
        public async Task PlaceABet(string betName, int betOptionId, int amount) 
        {
            float funds = await _coinService.GetFundsByUserId(Context.User.Id);
            if (amount > funds)
            {
                await ReplyAsync($"Not enough Attarcoins. Available: {funds} Attarcoins.");
            }
            else
            {
                Bettor bettor = await _betService.PlaceBet(Context.User.Id, amount, betOptionId, betName);
                if (bettor != null)
                {
                    await _coinService.RemoveFunds(Context.User.Id, amount);
                    Bet bet = await _betService.GetBetByName(betName);
                    await ReplyAsync(string.Empty, false, BetView.BetPlaced(bet, bettor, Context.User));
                }
                else
                {
                    await ReplyAsync("You can't place that bet.");
                }
            }
        }

        // RESOLVE

        [Command("bet-release")]
        [Summary("Release bet.")]
        public async Task Release(string betName)
        {
            if (_betService.ReleaseBet(Context.User.Id, betName) == null)
            {
                await ReplyAsync("That bet is no longer active or it doesn't longer exist.");
            }
            else
            {
                if (await _betService.CanWithdrawBet(Context.User.Id, betName))
                {
                    var bettor = await _betService.WithdrawBet(Context.User.Id, betName);
                    await ReplyAsync($"You have withdrawn {bettor.Amount} coins.");
                }
                else if (await _betService.CanPayout(betName))
                {
                    var rewards = await _betService.Payout(betName);
                    if (rewards.Count() > 0)
                    {
                        foreach (var reward in rewards)
                        {
                            await _coinService.AddFunds(reward.UserId, reward.Amount);
                        }

                        Bet bet = await _betService.GetBetByName(betName);
                        await ReplyAsync(string.Empty, false, BetView.BetResolved(bet, rewards, Optional<IUser>.Unspecified));
                    }
                    else
                    {
                        await ReplyAsync("No one is rewarded.");
                    }
                }
                else
                {
                    await ReplyAsync("You have released your bet.");
                }
            }
        }

        [RequiredJudgeAssigned]
        [JudgeCommand]
        [Command("bet-resolve")]
        [Summary("Override by the judge.")]
        public async Task Resolve(string betName, int betOptiondId)
        {
            Bet bet = await _betService.GetBetByName(betName);
            IEnumerable<BetReward> rewards = await _betService.Resolve(betName, betOptiondId);
            Optional<IUser> user = Optional.Create<IUser>(Context.User);
            if(bet != null)
            {
                Embed embed = BetView.BetResolved(bet, rewards, user);
                await ReplyAsync(string.Empty, false, embed);
            }
            else
            {
                await ReplyAsync($"Bet '{betName}' not found.");
            }
        }

        [RequiredBotAuthor]
        [Command("bet-judge")]
        public async Task AssignJudge(IUser judge)
        {
            var config = _dynamicConfigRepository.Get();
            if (config != null)
            {
                config.JudgeId = judge.Id;
                _dynamicConfigRepository.Update(config);
                await ReplyAsync($"{MentionUtils.MentionUser(judge.Id)} is the new judge.");
            }
            else
            {
                await ReplyAsync("There was an issue. Please contanct 'JJ 3maj'");
            }
        }

        [Command("bet-judge")]
        public async Task GetJudge()
        {
            var config = _dynamicConfigRepository.Get();
            if (config != null)
            {
                if (config.JudgeId > 0)
                    await ReplyAsync($"{MentionUtils.MentionUser(config.JudgeId)} is the judge.");
                else
                    await ReplyAsync("Judge still hasn't been appointed.");
            }
            else
            {
                await ReplyAsync("There was an issue. Judge couldn't be found.");
            }
        }
    }
}
