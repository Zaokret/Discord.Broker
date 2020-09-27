using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Core.Models
{
    public class Bet
    {
        public Bet(string name, string desc)
        {
            Name = name.Trim();
            Desc = desc.Trim();
            Resolved = false;
            Options = new List<BetOption>();
            Bettors = new List<Bettor>();
            Created = DateTime.Now;
        }

        public string Name { get; set; }
        public DateTime Created { get; set; }
        public string Desc { get; set; }
        public bool Resolved { get; set; }
        public IEnumerable<BetOption> Options { get; set; }
        public IEnumerable<Bettor> Bettors { get; set; }
    }

    public class Bettor
    {
        public ulong UserId { get; set; }
        public int Amount { get; set; }
        public int BetOptionId { get; set; }
        public bool Released { get; set; }
    }

    public class BetOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Odds { get; set; }
    }

    public class BetReward
    {
        public ulong UserId { get; set; }
        public int Amount { get; set; }
    }
}
