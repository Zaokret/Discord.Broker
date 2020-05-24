using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Game.CoinWar
{
    public class OptionalUtilities
    {
        public static Optional<T> Try<T>(T obj)
        {
            if (obj == null)
                return Optional<T>.Unspecified;
            return obj;
        }

        public static async Task<Optional<T>> TryAsync<T>(Task<T> task)
        {
            return Try(await task);
        }
    }
}
