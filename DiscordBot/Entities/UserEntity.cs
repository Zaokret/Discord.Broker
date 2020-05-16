using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Entities
{
  public class UserEntity
  {
    public UserEntity() { }
    public UserEntity(User user)
    {
      if (user == null)
        throw new ArgumentNullException(nameof(user));

      UserId = user.Id;
      Funds = user.Wallet.Funds;
    }
    public ulong UserId { get; set; }
    public float Funds { get; set; }
  }
}
