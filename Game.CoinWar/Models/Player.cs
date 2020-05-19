using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Models
{
    public class Player
    {
        public IUser User { get; set; }
        public int CurrentBet { get; set; }
        public int Coins { get; set; }
        public int TeamId { get; set; }
        public bool Winner { get; set; }

        public Player Win(int winningTeamId)
        {
            Winner = winningTeamId == TeamId;
            return this;
        }

        public bool IsBetValid(int bet)
        {
            return bet > 0 && (bet + CurrentBet) <= Coins;
        }

        public Player Bet(int bet)
        {
            CurrentBet += bet;
            return this;
        }

        public Player NextRound()
        {
            Coins -= CurrentBet;
            CurrentBet = 0;
            return this;
        }
    }
}
