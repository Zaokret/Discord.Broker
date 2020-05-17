using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot
{
  public class Configuration
  {
    public string Token { get; set; }
    public IEnumerable<Coin> Coins { get; set; }
    public char CommandPrefix { get; set; }
  }
}
