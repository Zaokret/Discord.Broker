using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Core.Extensions
{
    public static class MessageMentionUtils
    {
        private static readonly string BaseUrl = "https://discord.com/channels";

        public static string Url(ulong guildID, ulong channelID, ulong messageID)
        {
            return $"{BaseUrl}/{guildID}/{channelID}/{messageID}";
        }
    }
}
