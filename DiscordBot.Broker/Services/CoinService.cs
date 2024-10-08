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
        public IUser User { get; set; }
        public int Rank { get; set; }
        public int Points { get; set; }
    }

    public class LeaderboardView
    {
        public IEnumerable<RankedUser> TopUsers { get; set; }
        public RankedUser IssuerRanking { get; set; }
        public int TotalUsers { get; set; }
        public IUser TheInfinite { get; set; } // meme
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

        public async Task<int> GetFundsByUserId(ulong userId)
        {
            return (int)(await _repository.GetCoinsByUserIdAsync(userId));
        }

        private async Task<RankedUser> GetRankedUser(UserEntity entity, int rank, List<SocketGuildUser> users)
        {
            SocketGuildUser guildUser = users.FirstOrDefault(u => u.Id == entity.UserId);
            if(guildUser != null)
            {
                return new RankedUser
                {
                    User = guildUser,
                    Rank = rank,
                    Points = (int)entity.Funds
                };
            }

            IUser restUser = await _client.Rest.GetUserAsync(entity.UserId);
            if (restUser == null)
                return null;

            return new RankedUser
            {
                User = restUser,
                Rank = rank,
                Points = (int)entity.Funds
            };
        }

        public async Task<LeaderboardView> GetLeaderboard(ulong userId, List<SocketGuildUser> users)
        {
            IEnumerable<UserEntity> entities = await _repository.GetAllUsersAsync();
            var rankedEntities = entities
                .OrderByDescending(e => e.Funds)
                .ToList();

            var top5 = await Task.WhenAll(
                rankedEntities
                .Take(5)
                .Select((entity, index) => GetRankedUser(entity, index + 1, users)));

            UserEntity userEntity = rankedEntities.FirstOrDefault(e => e.UserId == userId);
            if(userEntity == null)
            {
                return new LeaderboardView
                {
                    TopUsers = top5,
                    IssuerRanking = null,
                    TotalUsers = rankedEntities.Count()
                };
            }

            int rank = rankedEntities.IndexOf(userEntity) + 1;
            RankedUser rankedUser = await GetRankedUser(userEntity, rank, users);

            return new LeaderboardView
            {
                TopUsers = top5,
                IssuerRanking = rankedUser,
                TotalUsers = rankedEntities.Count()
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
                return funds;
            }
        }

        public async Task<float> RemoveFunds(ulong userId, float funds)
        {
            UserEntity entity = await _repository.GetUserByIdAsync(userId);
            if (entity != null)
            {
                Wallet wallet = new Wallet(entity.Funds);
                wallet.Widthdraw(funds);
                await _repository.UpdateFundsAsync(userId, wallet.Funds);
                return wallet.Funds;
            }
            return 0;
        }

        public async Task<float> AddCoin(ulong userId, Coin coin)
        {
            return await AddFunds(userId, coin.Value);
        }

        public async Task<float> RemoveCoin(ulong userId, Coin coin)
        {
            return await RemoveFunds(userId, coin.Value);
        }

        public async Task SaveAsync()
        {
            await _repository.SaveAsync();
        }
    }
}
