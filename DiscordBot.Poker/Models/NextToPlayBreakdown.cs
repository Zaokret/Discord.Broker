using DiscordBot.Models;
using System.Collections.Generic;

namespace DiscordBot.Poker.Models
{
    public class NextToPlayBreakdown
    {
        public Player Player { get; set; }
        public Wallet Pot { get; set; }
        public List<Action> AvailableActions { get; set; }
        public List<Card> Community { get; set; }
    }
}
