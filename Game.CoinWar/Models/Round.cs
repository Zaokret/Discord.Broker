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
            BothTeamsLostRewards = false;
        }

        public Round(Player winner, Player loser, bool withoutFunds = false)
        {
            WinnerTeamId = winner.TeamId;
            BothTeamsLostRewards = false;
            Winner = winner.Copy();
            Loser = loser.Copy();
            LoserWithoutFunds = withoutFunds;
        }

        public static Round BothWithoutFunds()
        {
            return new Round(0)
            {
                BothTeamsLostRewards = true,
                LoserWithoutFunds = true
            };
        }

        public Player Winner { get; set; }
        public Player Loser { get; set; }
        public bool BothTeamsLostRewards { get; set; }
        public int WinnerTeamId { get; set; }
        public bool LoserWithoutFunds { get; set; }
    }
}
