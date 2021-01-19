using Discord;
using DiscordBot.Core.Models;
using DiscordBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Escrow
{
    public class BetService
    {
        private readonly BetRepository _repository;
        public BetService(BetRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<Bet>> GetBets(Func<Bet, bool> predicate)
        {
            return (await _repository.GetAll(predicate)).OrderByDescending(b => b.Created);
        }

        public async Task<IEnumerable<Bet>> GetTopBets()
        {
            return (await _repository.GetAll((bet) => true))
                .OrderByDescending(bet => 
                {
                    return bet.Bettors.Sum(bettor =>
                    {
                        float odds = bet.Options.FirstOrDefault(option => option.Id == bettor.BetOptionId).Odds;
                        return odds * bettor.Amount;
                    });
                });
        }

        public async Task<Bet> GetBetByName(string name)
        {
            return await _repository.GetBetByName(name);
        }

        public async Task<Bet> CreateBet(string desc, string name)
        {
            Bet bet = new Bet(name, desc);
            await _repository.AddBet(bet);
            await _repository.SaveAsync();
            return bet;
        }

        public async Task<BetOption> AddBetOption(string name, float odds, string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null)
                return null;

            if (bet.Resolved)
                return null;

            if (odds <= 1)
                return null;

            BetOption newOption = new BetOption
            {
                Id = bet.Options.Count() + 1,
                Name = name,
                Odds = odds
            };

            bet.Options = bet.Options.Concat(new[] { newOption });
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();
            return newOption;
        }

        public async Task<bool> IsPlacingMultipleBetOptionsAsync(ulong userId, string betName, int betOptionId)
        {
            Bet bet = await _repository.GetBetByName(betName);
            return IsPlacingMultipleBetOptions(userId, bet, betOptionId);
        }

        public bool IsPlacingMultipleBetOptions(ulong userId, Bet bet, int betOptionId)
        {
            return bet.Bettors.Any(b => b.UserId == userId && b.BetOptionId != betOptionId);
        }

        public async Task<Bettor> PlaceBet(ulong userId, int amount, int betOptionId, string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null || bet.Resolved || !bet.Options.Any(o => o.Id == betOptionId) || IsPlacingMultipleBetOptions(userId, bet, betOptionId))
            {
                return null;
            }

            Bettor bettor = bet.Bettors.FirstOrDefault(b => b.UserId == userId && b.BetOptionId == betOptionId);
            if(bettor != null)
            {
                bettor.Amount += amount;
            } 
            else
            {
                bettor = new Bettor
                {
                    UserId = userId,
                    Amount = amount,
                    BetOptionId = betOptionId,
                    Released = false
                };
                bet.Bettors = bet.Bettors.Concat(new[] { bettor });
            }
            
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();
            return bettor;
        }

        public async Task<Bettor> ReleaseBet(ulong userId, string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null || bet.Resolved)
            {
                return null;
            }

            var bettor = bet.Bettors.FirstOrDefault(b => b.UserId == userId);
            bettor.Released = true;
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();
            return bettor;
        }

        public async Task<bool> CanPayout(string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null || bet.Resolved)
            {
                return false;
            }

            return bet.Bettors
                .GroupBy(b => b.BetOptionId)
                .Count(g => g.Any(b => !b.Released)) == 1;
        }

        public async Task<bool> CanWithdrawBet(ulong userId, string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null || bet.Resolved || !bet.Bettors.Any(b => b.UserId == userId))
            {
                return false;
            }
            return bet.Options.Count() == 1 || bet.Bettors.Count() == 1;
        }

        public async Task<Bettor> WithdrawBet(ulong userId, string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if(bet == null || bet.Resolved)
            {
                return null;
            }
            var usersBet = bet.Bettors.FirstOrDefault(bettor => bettor.UserId == userId);
            bet.Bettors = bet.Bettors.Where(bettor => bettor.UserId != userId);
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();
            return usersBet;
        }

        public async Task<IEnumerable<BetReward>> Payout(string betName)
        {
            Bet bet = await _repository.GetBetByName(betName);
            if (bet == null || bet.Resolved)
            {
                return new List<BetReward>();
            }
                
            var winners = bet.Bettors
                .GroupBy(b => b.BetOptionId)
                .FirstOrDefault(g => g.Any(b => !b.Released));

            var betOption = bet.Options.FirstOrDefault(o => o.Id == winners.Key);
            bet.Resolved = true;
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();

            return winners
                .Select(w => new BetReward
                {
                    UserId = w.UserId,
                    Amount = (int)Math.Round(betOption.Odds * w.Amount)
                });
        }

        public async Task<IEnumerable<BetReward>> Resolve(string betName, int betOptionId)
        {
            Bet bet = await _repository.GetBetByName(betName);
            BetOption option = bet.Options.FirstOrDefault(o => o.Id == betOptionId);
            if (bet == null || bet.Resolved || option == null)
            {
                return new List<BetReward>();
            }

            bet.Resolved = true;
            await _repository.UpdateBet(bet);
            await _repository.SaveAsync();

            return bet.Bettors
                .Where(bettor => bettor.BetOptionId == betOptionId)
                .Select(w => new BetReward
                    {
                        UserId = w.UserId,
                        Amount = (int)Math.Round(option.Odds * w.Amount)
                    });
        }

        public async Task<Bet> CreateQuickBet(IUser user, int amount)
        {
            float quickOdds = 2;
            Bet bet = new Bet(Guid.NewGuid().ToString(), string.Empty)
            {
                Options = new[]
                {
                    new BetOption
                    {
                        Id = 1,
                        Name = "1",
                        Odds = quickOdds
                    },
                    new BetOption
                    {
                        Id = 2,
                        Name = "2",
                        Odds = quickOdds
                    }
                },
                Bettors = new []
                {
                    new Bettor()
                    {
                        Amount = amount,
                        UserId = user.Id,
                        BetOptionId = 1,
                        Released = false
                    }
                }
            };

            if((await GetBetByName(bet.Name)) != null)
            {
                return await CreateQuickBet(user, amount);
            }

            await _repository.AddBet(bet);
            await _repository.SaveAsync();
            return bet;
        }

        public async Task<Bet> CreateQuickBet(string betName, List<string> options)
        {
            float quickOdds = 2;
            Bet bet = new Bet(betName, string.Empty)
            {
                Options = options.Select((option, index) => new BetOption() { Id = index + 1, Name = option, Odds = quickOdds}),
            };

            await _repository.AddBet(bet);
            await _repository.SaveAsync();
            return bet;
        }
    }
}
