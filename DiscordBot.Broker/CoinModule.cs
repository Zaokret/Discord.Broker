using Discord;
using Discord.Commands;
using DiscordBot.Contracts;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
  public class CoinModule : ModuleBase<SocketCommandContext>
  {
    private readonly IUserRepository _userRepository;
    public CoinModule(IUserRepository userRepository)
    {
      _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    [Command("coins")]
    [Summary("Retreives coins for user or issuer.")]
    public async Task GetCoinsAsync([Summary("User to get coins for.")] IUser user = null)
    {
      user = user ?? Context.User;
      if(!(await _userRepository.UserExistAsync(user.Id)))
      {
        await ReplyAsync($"Fella is off the books.");
      }
      else
      {
        float coins = await _userRepository.GetCoinsByUserIdAsync(user.Id);
        int rounded = (int)Math.Round(coins);
        await ReplyAsync(GetUserMessage(rounded, user.Username));
      }
    }

    private string GetUserMessage(int coins, string username)
    {
      if (coins == 0)
        return $"{username} is empty-handed.";
      if (coins == 1)
        return $"{username} has one Attarcoin.";

      return $"{username} has {coins} Attarcoins.";
    }

  }
}
