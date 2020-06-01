using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Models
{
    public enum PhaseType
    {
        Night,
        Investigation,
        Day
    }

    public class GamePhase
    {
        public PhaseType Phase { get; set; }
        public IUser Target { get; set; }
    }
}
