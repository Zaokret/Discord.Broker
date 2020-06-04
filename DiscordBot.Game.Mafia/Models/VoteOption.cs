using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Models
{
    public class VoteOption
    {
        public VoteOption(string emoteName, ulong userId)
        {
            EmoteName = emoteName;
            UserId = userId;
        }

        public VoteOption(string emoteName) : this(emoteName, default(ulong)) { }

        public string EmoteName { get; set; }
        public ulong UserId { get; set; }
    }
}
