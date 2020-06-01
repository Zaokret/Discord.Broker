using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Models
{
    public enum GroupType
    {
        Informed,
        Uninformed
    }
    
    public enum GameRole
    {
        Killer,
        Kicker,
        Investigator
    }

    public class Player
    {
        public Player(IUser user, GroupType group, GameRole role)
        {
            User = user;
            Group = group;
            Role = role;
        }
        public IUser User { get; set; }
        public GroupType Group { get; set; }
        public GameRole Role { get; set; }
        public bool Active { get; set; }
    }
}
