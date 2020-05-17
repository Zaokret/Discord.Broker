using Discord;
using DiscordBot.Contexts;
using DiscordBot.Contracts;
using DiscordBot.Entities;
using DiscordBot.Enums;
using DiscordBot.Extensions;
using DiscordBot.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
  public class JsonUserRepository : IUserRepository
  {
    private readonly UserEntityContextProvider _contextProvider;
    public JsonUserRepository(UserEntityContextProvider contextProvider)
    {
      _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    public async Task SaveAsync()
    {
      await _contextProvider.SaveUserJsonArray();
    }

    public async Task AddUserAsync(User user)
    {
      JArray userTokenArray = await _contextProvider.GetUserJsonArray();
      if (!UserExist(userTokenArray, user.Id))
      {
        userTokenArray.Add(JToken.FromObject(new UserEntity(user.Id, user.Wallet.Funds)));
      }
    }

    public async Task AddUserAsync(ulong userId)
    {
      JArray userTokenArray = await _contextProvider.GetUserJsonArray();
      if(!UserExist(userTokenArray, userId))
      {
        userTokenArray.Add(JToken.FromObject(new UserEntity(userId, 0)));
      }
    }

    public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
    {
      return (await _contextProvider.GetUserJsonArray()).ToObject<IEnumerable<UserEntity>>();
    }

    public async Task UpdateFundsAsync(ulong userId, float newFunds)
    {
      JArray userTokenArray = await _contextProvider.GetUserJsonArray();
      string fundsProperty = Enum.GetName(typeof(EntityTokenProperties), EntityTokenProperties.Funds);
      userTokenArray.Where(ByToken(EntityTokenProperties.UserId, userId)).Select(token =>
      {
        token[fundsProperty] = newFunds;
        return token;
      }).ToList();
    }
    
    public async Task<float> GetCoinsByUserIdAsync(ulong userId)
    {
      UserEntity user = await GetUserByIdAsync(userId);
      return user?.Funds ?? 0;
    }

    private Func<JToken, bool> ByToken<T> (EntityTokenProperties prop, T val) where T : IEquatable<T>
    {
      if (!Enum.IsDefined(typeof(EntityTokenProperties), prop))
      {
        throw new Exception($"Enum property {prop} is not defined.");
      }
      return t =>
      {
        JToken token = t.SelectToken(Enum.GetName(typeof(EntityTokenProperties), prop));
        if (token == null)
          return false;
        return EqualityComparer<T>.Default.Equals(token.Value<T>(), val);
      };
    }

    public async Task<UserEntity> GetUserByIdAsync(ulong userId)
    {
      JArray userTokenArray = await _contextProvider.GetUserJsonArray();
      JToken userToken = userTokenArray.FirstOrDefault(ByToken(EntityTokenProperties.UserId, userId));
      return userToken?.ToObject<UserEntity>();
    }

    public async Task<bool> UserExistAsync(ulong userId)
    {
      JArray userTokenArray = await _contextProvider.GetUserJsonArray();
      return UserExist(userTokenArray, userId);
    }

    public bool UserExist(JArray userTokenArray, ulong userId)
    {
      return userTokenArray.Any(ByToken(EntityTokenProperties.UserId, userId));
    }
  }
}
