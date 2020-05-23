using Discord;
using DiscordBot.Core.Models;
using DiscordBot.Game.CoinWar;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    class GuidHelper
    {
        public static string Generate()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
    
    public class PendingGame
    {
        public PendingGame(IUser initiator, IUser challanged, CollectableEntity collectable)
        {
            Guid = GuidHelper.Generate();
            CreatedDate = DateTime.Now;
            Initiator = initiator;
            Challanged = challanged;
            Started = false;
            Collectable = collectable;
        }
        public CollectableEntity Collectable { get; set; }
        public DateTime CreatedDate { get; set; } 
        public string Guid { get; set; }
        public IUser Initiator { get; set; }
        public IUser Challanged { get; set; }
        public bool Started { get; set; }
    }
}
