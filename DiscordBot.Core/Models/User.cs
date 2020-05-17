using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
  public class User
  {
    public User() { }
    public User(ulong userId, Wallet wallet)
    {
      Wallet = wallet;
      Id = userId;
    }
    public Wallet Wallet { get; set; }
    public ulong Id { get; set; }

    public override string ToString()
    {
      return $"User with id {Id} has wallet {Wallet}.";
    }
  }
}
