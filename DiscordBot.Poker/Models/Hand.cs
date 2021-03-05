using DiscordBot.Models;
using DiscordBot.Poker.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Poker.Models
{
    public class Hand
    {
        public Hand(Queue<Player> players)
        {
            Pot = new Wallet(0); // initialize pot first
            Players = players.ToList();
            Community = new List<Card>();
            Turn = new Turn(players);
            SetButtonAndBlinds();
            Deck = new Deck();
            Deck.Suffle();
            Players = players.Select(p =>
            {
                p.Hole = Deck.Draw(2).ToList();
                return p;
            }).ToList();
        }

        public float Blind { get; set; } = 2;
        public Turn Turn { get; set; }
        public List<Player> Players { get; set; }
        public Wallet Pot { get; set; }
        public List<Card> Community { get; set; }
        public Deck Deck { get; set; }

        public Player Playing()
        {
            return Players.FirstOrDefault(p => p.UserId == Turn.Playing().UserId);
        }

        public bool CanCall()
        {
            Player p = Playing();
            float difference = CurrentBid() - p.Bet.Funds;
            return p.Wallet.CanWithdraw(difference);
        }

        public bool CanRaise(int amount)
        {
            Player p = Playing();
            return p.Wallet.CanWithdraw(amount);
        }

        public bool HasFunds()
        {
            Player p = Playing();
            return p.Wallet.Funds != 0;
        }

        public bool IsOver()
        {
            return Turn.IsOver() && Community.Count == 5;
        }

        public void NextTurn()
        {
            HandStage currentStage = (HandStage)Community.Count;
            switch (currentStage)
            {
                case HandStage.PreFlop:
                    Turn.UpdateOrder(Players);
                    Community = Deck.Draw(3).ToList();
                    break;
                case HandStage.Flop:
                    Turn.UpdateOrder(Players);
                    Community.Add(Deck.Draw(1).First());
                    break;
                case HandStage.TheTurn:
                    Turn.UpdateOrder(Players);
                    Community.Add(Deck.Draw(1).First());
                    break;
                case HandStage.River:
                    // showdown
                    break;
                default: throw new InvalidOperationException("NextTurn failed because current stage is invalid.");
            }

        }

        public void SetButtonAndBlinds()
        {
            Check();
            Turn.PlayCount = 0;

            if (Players.Count() == 2)
            {
                Raise(Blind);
                Turn.PlayCount = 0;

                Raise(Blind / 2);
                Turn.PlayCount = 0;
            }
            else
            {
                Raise(Blind / 2);
                Turn.PlayCount = 0;

                Raise(Blind);
                Turn.PlayCount = 0;
            }
        }

        /* HELPERS */

        public bool DidEveryoneCallTheBet()
        {
            return Players.GroupBy(p => p.Bet.Funds).Select(p => p.FirstOrDefault()).Count() == 1;
        }

        public float LastRaise()
        {
            Player[] highestBiders = Players.OrderByDescending(p => p.Bet.Funds).Take(2).ToArray();
            return highestBiders[0].Bet.Funds - highestBiders[1].Bet.Funds;
        }

        public float CurrentBid()
        {
            Player highestBider = Players.OrderByDescending(p => p.Bet.Funds).FirstOrDefault();
            return highestBider.Bet.Funds;
        }

        public float MinRaise()
        {
            return LastRaise() * 2;
        }

        // move this check to game obj
        public bool IsValidRaise(float amount)
        {
            return amount * 2 >= LastRaise();
        }

        /* ACTIONS */

        public void Leave(ulong userid)
        {
            if (Playing().UserId == userid)
            {
                Fold();
            }
            else
            {
                Turn.Remove(userid);
            }
            Players = Players.Where(p => p.UserId != userid).ToList();
        }

        public Player Fold()
        {
            Players = Players.Where(p => p.UserId == Playing().UserId).ToList();
            return Turn.Next(folded: true);
        }

        public Player Check()
        {
            return Turn.Next();
        }

        public Player Call()
        {
            Player p = Playing();
            float difference = CurrentBid() - p.Bet.Funds;
            p.Wallet.Widthdraw(difference);
            p.Bet.Deposit(difference);
            Pot.Deposit(difference);
            return Turn.Next();
        }

        public Player Raise(float amount)
        {
            Player p = Playing();

            p.Wallet.Widthdraw(amount);
            p.Bet.Deposit(amount);
            Pot.Deposit(amount);

            return Turn.Next();
        }

        public Player AllIn()
        {
            Player p = Playing();
            p.Bet.Deposit(p.Wallet.Empty());
            return Turn.Next();
        }

    }

}
