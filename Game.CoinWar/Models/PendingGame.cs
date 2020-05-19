using DiscordBot.Game.CoinWar;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class PendingGame
    {
        public PendingGame(ulong userId)
        {
            Guid = GuidHelper.Generate();
            UserInitiatorId = userId;
        }
        public string Guid { get; set; }
        public ulong UserInitiatorId { get; set; }
    }
}
