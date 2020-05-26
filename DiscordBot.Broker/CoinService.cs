using Discord;
using Discord.WebSocket;
using DiscordBot.Contracts;
using DiscordBot.Entities;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    public class RankedUser
    {
        public NamedUser User { get; set; }
        public int Rank { get; set; }
        public int Points { get; set; }
    }

    public class LeaderboardView
    {
        public IEnumerable<RankedUser> TopUsers { get; set; }
        public RankedUser IssuerRanking { get; set; }
        public int TotalUsers { get; set; }
    }

    public class CoinService
    {
        public readonly IUserRepository _repository;
        private readonly DiscordSocketClient _client;

        public CoinService(IUserRepository repository, DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<LeaderboardView> GetLeaderboard(ulong userId, List<NamedUser> users)
        {
            IEnumerable<UserEntity> entities = await _repository.GetAllUsersAsync();

            List<RankedUser> rankedUsers = entities
                .OrderByDescending(e => e.Funds)
                .Select((entity, index) =>
                {
                    return new RankedUser()
                    {
                        User = users.FirstOrDefault(g => g.Id == entity.UserId),
                        Points = (int)entity.Funds,
                        Rank = index + 1,
                    };
                })
                .ToList();

            return new LeaderboardView
            {
                TopUsers = rankedUsers.Take(5),
                IssuerRanking = rankedUsers.FirstOrDefault(r => r.User.Id == userId),
                TotalUsers = rankedUsers.Count
            };
        }

        public async Task<float> AddFunds(ulong userId, float funds)
        {
            UserEntity entity = await _repository.GetUserByIdAsync(userId);
            if (entity != null)
            {
                Wallet wallet = new Wallet(entity.Funds);
                wallet.Deposit(funds);
                await _repository.UpdateFundsAsync(userId, wallet.Funds);
                await _repository.SaveAsync();
                return wallet.Funds;
            }
            else
            {
                UserEntity user = new UserEntity
                {
                    UserId = userId,
                    Funds = funds
                };
                await _repository.AddUserAsync(user);
                await _repository.SaveAsync();
                return funds;
            }
        }

        public async Task<float> AddCoin(ulong userId, Coin coin)
        {
            return await AddFunds(userId, coin.Value);
        }

        public async Task<float> RemoveCoin(ulong userId, Coin coin)
        {
            UserEntity entity = await _repository.GetUserByIdAsync(userId);
            if (entity != null)
            {
                Wallet wallet = new Wallet(entity.Funds);
                wallet.Widthdraw(coin.Value);
                await _repository.UpdateFundsAsync(userId, wallet.Funds);
                await _repository.SaveAsync();
                return wallet.Funds;
            }
            return 0;
        }

    }
}
