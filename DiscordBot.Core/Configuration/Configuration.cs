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
        public ulong AwardsChannelID { get; set; }
        public ulong ModeratorRoleID { get; set; }
        public int AwardCoinAmount { get; set; }
        public int PinCoinAmount { get; set; }
    }

    public class GlobalConfiguration : JsonConfiguration
    {
        public GlobalConfiguration(string discordToken, string tasteToken, JsonConfiguration jc)
        {
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new ArgumentNullException(nameof(discordToken));

            if (string.IsNullOrWhiteSpace(tasteToken))
                throw new ArgumentNullException(nameof(tasteToken));

            Coins = jc.Coins;
            CommandPrefix = jc.CommandPrefix;
            DiscordToken = discordToken;
            PayPalUrl = jc.PayPal;
            TasteToken = tasteToken;
            TestCommandPrefix = jc.TestCommandPrefix;
            AwardsChannelID = jc.AwardsChannelID;
            AwardCoinAmount = jc.AwardCoinAmount;
            PinCoinAmount = jc.PinCoinAmount;
            ModeratorRoleID = jc.ModeratorRoleID;
        }

        public ulong BotAuthor = 563437347899965455;
        public string DiscordToken { get; set; }
        public string PayPalUrl { get; set; }
        public string TasteToken { get; set; }
    }
}
