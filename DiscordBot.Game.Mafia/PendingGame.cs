using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia
{
    public class PendingGame
    {
        public PendingGame(IUser initiator)
        {
            Users = new List<IUser>(new[] { initiator });
            Created = DateTime.Now;
            Id = Guid.NewGuid().ToString("N");
            Active = false;
        }
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public ICollection<IUser> Users { get; set; }
        public bool Active { get; set; }
    }
}
