using DiscordBot.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Contracts
{
  public interface IUserRepository
  {
    Task<bool> UserExist(ulong userId);
    Task AddUser(ulong userId);
    Task<UserEntity> GetUserById(ulong userId);
    Task<IEnumerable<UserEntity>> GetAllUsers();

    Task<float> GetCoinsByUserId(ulong userId);
    Task AddCoin(ulong userId, float coinValue);
    Task SubtractCoin(ulong userId, float coinValue);

    Task SaveAsync();
  }
}
