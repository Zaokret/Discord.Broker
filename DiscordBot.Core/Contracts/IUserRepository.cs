using DiscordBot.Entities;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Contracts
{
  public interface IUserRepository
  {
    Task<bool> UserExistAsync(ulong userId);
    Task AddUserAsync(ulong userId);
    Task AddUserAsync(UserEntity user);
    Task UpdateFundsAsync(ulong userId, float newFunds);
    Task<UserEntity> GetUserByIdAsync(ulong userId);
    Task<IEnumerable<UserEntity>> GetAllUsersAsync();
    Task<float> GetCoinsByUserIdAsync(ulong userId);
    Task SaveAsync();
  }
}
