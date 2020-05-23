using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DiscordBot.Game.CoinWar.Extensions
{
    public static class StringExtensions
    {
        public static string Capitalize(this string str)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str);
        }
    }
}
