using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class Round
    {
        public Round(int teamId)
        {
            WinnerTeamId = teamId;
            BothTeamLost = false;
        }

        public static Round BothLost()
        {
            return new Round(0) { BothTeamLost = true };
        }

        public bool BothTeamLost { get; set; }
        public int WinnerTeamId { get; set; }
    }
}
