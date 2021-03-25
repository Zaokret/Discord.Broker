using DiscordBot.Poker.Helpers;
using System.Collections.Generic;

namespace DiscordBot.Poker.Models
{
    public class Showdown
    {
        public IEnumerable<PlayerHand> Hands { get; set; }
        public IEnumerable<Reward> Rewards { get; set; }
        public PlayerHand Winner { get; set; }
    }
}
