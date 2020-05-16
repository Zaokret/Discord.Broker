using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
  public class Wallet
  {
    public Wallet(float startingFunds)
    {
      Funds = startingFunds;
    }
    public float Funds { get; set; }

    public Wallet Deposit(float amount)
    {
      Funds += amount;
      return this;
    }

    public Wallet Widthdraw(float amount)
    {
      if ((Funds - amount) >= 0)
      {
        Funds -= amount;
      }
      return this;
    }

    public override string ToString()
    {
      return $"{Funds} funds available";
    }
  }
}
