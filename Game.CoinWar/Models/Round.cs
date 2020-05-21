using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class Round
    {
        public Round(int teamid)
        {
            WinnerTeamId = teamid;
            BothTeamLost = false;
        }

        public Round(Player winner, Player loser)
        {
            WinnerTeamId = winner.TeamId;
            BothTeamLost = false;
            Winner = winner.Copy();
            Loser = loser.Copy();
        }

        public static Round BothLost()
        {
            return new Round(0) { BothTeamLost = true };
        }

        public Player Winner { get; set; }
        public Player Loser { get; set; }
        public bool BothTeamLost { get; set; }
        public int WinnerTeamId { get; set; }
    }
}
