using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia.Models
{
    public enum MonitorType
    {
        Kick,
        Kill,
        Ready
    }

    public class VoteMonitor
    {
        public VoteMonitor(
            Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> monitor,
            ICollection<VoteOption> voteOptions)
        {
            Monitor = monitor;
            VoteOptions = voteOptions;
        }
        public Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> Monitor { get; set; }
        public ICollection<VoteOption> VoteOptions { get; set; }
    }
}
