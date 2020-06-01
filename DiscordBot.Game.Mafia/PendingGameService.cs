using DiscordBot.Game.Mafia.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia
{
    public static class PendingGameService
    {
        public static List<PendingGame> PendingGames = new List<PendingGame>();
        public static int ExpirationInMinutes = TimeSpan.FromMinutes(60).Minutes;
    }
}
