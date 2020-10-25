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
        public char TestCommandPrefix { get; set; }
    }

    public class GlobalConfiguration : JsonConfiguration
    {
        public GlobalConfiguration(string discordToken, string tasteToken, JsonConfiguration jsonConfig)
        {
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new ArgumentNullException(nameof(discordToken));

            if (string.IsNullOrWhiteSpace(tasteToken))
                throw new ArgumentNullException(nameof(tasteToken));

            Coins = jsonConfig.Coins;
            CommandPrefix = jsonConfig.CommandPrefix;
            DiscordToken = discordToken;
            PayPalUrl = jsonConfig.PayPal;
            TasteToken = tasteToken;
            TestCommandPrefix = jsonConfig.TestCommandPrefix;
        }

        public ulong BotAuthor = 563437347899965455;
        public string DiscordToken { get; set; }
        public string PayPalUrl { get; set; }
        public string TasteToken { get; set; }
    }
}
