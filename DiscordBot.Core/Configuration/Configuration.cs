using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot
{
    public class JsonConfiguration
    {
        public IEnumerable<Coin> Coins { get; set; }
        public char CommandPrefix { get; set; }
    }

    public class GlobalConfiguration : JsonConfiguration
    {
        public GlobalConfiguration(string token, JsonConfiguration jsonConfig)
        {
            Coins = jsonConfig.Coins;
            CommandPrefix = jsonConfig.CommandPrefix;
            Token = token;
        }
        public ulong BotAuthor = 563437347899965455;
        public string Token { get; set; }
    }
}
