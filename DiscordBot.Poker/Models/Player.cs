using DiscordBot.Models;
using System.Collections.Generic;

namespace DiscordBot.Poker.Models
{
    public class Player
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public List<Card> Hole { get; set; }
        public Wallet Wallet { get; set; }
        public Wallet Bet { get; set; }
        public bool HasButton { get; set; }
    }
}
