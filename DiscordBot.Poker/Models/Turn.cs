using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Poker.Models
{
    public class Turn
    {
        public Turn(Queue<Player> players)
        {
            Players = players;
            PlayCount = 0;
        }
        public Queue<Player> Players { get; set; }
        public int PlayCount { get; set; }

        public void UpdateOrder(List<Player> order)
        {
            if (order.Count != Players.Count)
            {
                throw new Exception("UpdateOrder failed.");
            }

            Players = new Queue<Player>(order);
            PlayCount = 0;
        }

        public Player Playing()
        {
            return Players.Peek();
        }

        public void PutAtBeginingOfQue(ulong userid)
        {
            while (Playing().UserId != userid)
            {
                Next();
                PlayCount = 0;
            }
        }

        public bool IsOver()
        {
            return PlayCount >= Players.Count;
        }

        public Player Next(bool folded = false)
        {
            PlayCount++;
            if (folded)
            {
                Players.Dequeue();
            }
            else
            {
                Players.Enqueue(Players.Dequeue());
            }

            if (IsOver())
            {
                return null;
            }

            return Players.Peek();
        }

        public void Remove(ulong userid)
        {
            Players = new Queue<Player>(Players.Where(p => p.UserId != userid));
        }
    }

}
