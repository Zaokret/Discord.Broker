using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot
{
    public class JsonConfiguration
    {
        public IEnumerable<Coin> Coins { get; set; }
        public string PayPal { get; set; }
        public char CommandPrefix { get; set; }
    }

    public class GlobalConfiguration : JsonConfiguration
    {
        public GlobalConfiguration(string token, JsonConfiguration jsonConfig)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            Coins = jsonConfig.Coins;
            CommandPrefix = jsonConfig.CommandPrefix;
            Token = token;
            PayPalUrl = jsonConfig.PayPal;
        }
        public ulong BotAuthor = 563437347899965455;
        public string Token { get; set; }
        public string PayPalUrl { get; set; }
    }
}
