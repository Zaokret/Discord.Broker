using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Core.Utilities
{
    public static class Mention
    {
        public static string Of(ulong userId)
        {
            return $"<@{userId}>";
        }
    }
}
