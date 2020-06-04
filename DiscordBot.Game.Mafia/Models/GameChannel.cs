using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Models
{
    public class GameChannel
    {
        public GameChannel(RestTextChannel channel, IEnumerable<string> commands)
        {
            Channel = channel;
            Commands = commands;
        }

        public RestTextChannel Channel { get; set; }
        public IEnumerable<string> Commands { get; set; }
    }
}
