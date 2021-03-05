using DiscordBot.Poker.Enums;
using DiscordBot.Poker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Poker
{
    public class HandHierarchyService
    {
        /*
            StraightFlush,
            FourOfAKind,
            FullHouse,
            Flush,
            Straight,
            ThreeOfAKind,
            TwoPair,
            OnePair,
            HighCard
         */

        private static void Categorize(IEnumerable<Card> cards)
        {
            var rankGroups = cards.GroupBy(c => c.Rank);
            var suitGroups = cards.GroupBy(c => c.Suit);

            List<Rank> allRanks = ((IEnumerable<Rank>)Enum.GetValues(typeof(Rank))).ToList();
            List<Rank> cardRankList = cards.Select(c => c.Rank).OrderBy(r => (int)r).ToList();
            /*
             * 1 2 3 4
             * 2 3
             * 
             * 2
             */

            List<Rank> straight = new List<Rank>();
            for (int i = 0; i < cardRankList.Count; i++)
            {
                var s1_index = allRanks.IndexOf(cardRankList[i]);
                for (int j = 0; j < cardRankList.Count; j++)
                {
                    if(allRanks[j + s1_index] == cardRankList[j])
                    {
                        straight.Add(cardRankList[j]);
                    }
                }
                if(straight.Count == 5)
                {
                    return straight;
                }
                straight.Clear();
            }

            return null;

            

        }
       
        public static Card HighCard(IEnumerable<Card> cards)
        {
            return cards.OrderBy(c => (int)c.Rank).FirstOrDefault();
        }

        public static bool IsPair(IEnumerable<Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Count(g => g.Count() >= 2) >= 1;
        }

        public static bool IsThreeOfAKind(IEnumerable<Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() == 3);
        }

        public static bool IsTwoPairs(IEnumerable<Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Count(g => g.Count() >= 2) >= 2;
        }

        public static bool IsFlush(IEnumerable<Card> cards)
        {
            return cards.GroupBy(c => c.Suit).Any(g => g.Count() >= 5);
        }

        public static bool IsFullHouse(IEnumerable<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).OrderByDescending(c => c.Count()).Take(2).ToList();
            return groups[0].Count() == 3 && groups[1].Count() == 2;
        }

        public static bool IsFourOfAKind(IEnumerable<Card> cards)
        {
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() == 4);
        }

        public static bool IsStraightFlush(IEnumerable<Card> cards)
        {
            return false;
        }

        public static bool IsRoyalFlush(IEnumerable<Card> cards)
        {
            return false;
        }
    }
}
