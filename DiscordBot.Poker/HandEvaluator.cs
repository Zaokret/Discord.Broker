using DiscordBot.Poker.Enums;
using DiscordBot.Poker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Poker
{
    public class HandEvaluator
    {
        private const int ComparableCards = 5;

        /// <summary>
        /// Finds the best possible hand given a player's cards and all revealed comunity cards.
        /// </summary>
        /// <param name="cards">A player's cards + all revealed comunity cards (at lesat 5 in total).</param>
        /// <returns>Returns an object of type BestHand.</returns>
        public BestHand GetBestHand(IEnumerable<Card> cards)
        {
            var cardSuitCounts = new int[(int)Suit.Spade + 1];
            var cardTypeCounts = new int[(int)Rank.Ace + 1];
            foreach (var card in cards)
            {
                cardSuitCounts[(int)card.Suit]++;
                cardTypeCounts[(int)card.Rank]++;
            }

            // Flushes
            if (cardSuitCounts.Any(x => x >= ComparableCards))
            {
                // Straight flush
                var straightFlushCards = this.GetStraightFlushCards(cardSuitCounts, cards);
                if (straightFlushCards.Count > 0)
                {
                    return new BestHand(HandRank.StraightFlush, straightFlushCards);
                }

                // Flush - it is not possible to have Flush and either Four of a kind or Full house at the same time
                for (var i = 0; i < cardSuitCounts.Length; i++)
                {
                    if (cardSuitCounts[i] >= ComparableCards)
                    {
                        var flushCards =
                            cards.Where(x => x.Suit == (Suit)i)
                                .Select(x => x.Rank)
                                .OrderByDescending(x => x)
                                .Take(ComparableCards)
                                .ToList();
                        return new BestHand(HandRank.Flush, flushCards);
                    }
                }
            }

            // Four of a kind
            if (cardTypeCounts.Any(x => x == 4))
            {
                var bestFourOfAKind = this.GetTypesWithNCards(cardTypeCounts, 4)[0];
                var bestCards = new List<Rank>
                                    {
                                        bestFourOfAKind,
                                        bestFourOfAKind,
                                        bestFourOfAKind,
                                        bestFourOfAKind,
                                        cards.Where(x => x.Rank != bestFourOfAKind).Max(x => x.Rank),
                                    };

                return new BestHand(HandRank.FourOfAKind, bestCards);
            }

            // Full
            var pairTypes = this.GetTypesWithNCards(cardTypeCounts, 2);
            var threeOfAKindTypes = this.GetTypesWithNCards(cardTypeCounts, 3);
            if ((pairTypes.Count > 0 && threeOfAKindTypes.Count > 0) || threeOfAKindTypes.Count > 1)
            {
                var bestCards = new List<Rank>();
                for (var i = 0; i < 3; i++)
                {
                    bestCards.Add(threeOfAKindTypes[0]);
                }

                if (threeOfAKindTypes.Count > 1)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        bestCards.Add(threeOfAKindTypes[1]);
                    }
                }

                if (pairTypes.Count > 0)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        bestCards.Add(pairTypes[0]);
                    }
                }

                return new BestHand(HandRank.FullHouse, bestCards);
            }

            // Straight
            var straightCards = this.GetStraightCards(cardTypeCounts);
            if (straightCards != null)
            {
                return new BestHand(HandRank.Straight, straightCards);
            }

            // 3 of a kind
            if (threeOfAKindTypes.Count > 0)
            {
                var bestThreeOfAKindType = threeOfAKindTypes[0];
                var bestCards =
                    cards.Where(x => x.Rank != bestThreeOfAKindType)
                        .Select(x => x.Rank)
                        .OrderByDescending(x => x)
                        .Take(ComparableCards - 3).ToList();
                bestCards.AddRange(Enumerable.Repeat(bestThreeOfAKindType, 3));

                return new BestHand(HandRank.ThreeOfAKind, bestCards);
            }

            // Two pairs
            if (pairTypes.Count >= 2)
            {
                var bestCards = new List<Rank>
                                    {
                                        pairTypes[0],
                                        pairTypes[0],
                                        pairTypes[1],
                                        pairTypes[1],
                                        cards.Where(x => x.Rank != pairTypes[0] && x.Rank != pairTypes[1])
                                            .Max(x => x.Rank),
                                    };
                return new BestHand(HandRank.TwoPairs, bestCards);
            }

            // Pair
            if (pairTypes.Count == 1)
            {
                var bestCards =
                    cards.Where(x => x.Rank != pairTypes[0])
                        .Select(x => x.Rank)
                        .OrderByDescending(x => x)
                        .Take(3).ToList();
                bestCards.Add(pairTypes[0]);
                bestCards.Add(pairTypes[0]);
                return new BestHand(HandRank.Pair, bestCards);
            }
            else
            {
                // High card
                var bestCards = new List<Rank>();
                for (var i = cardTypeCounts.Length - 1; i >= 0; i--)
                {
                    if (cardTypeCounts[i] > 0)
                    {
                        bestCards.Add((Rank)i);
                    }

                    if (bestCards.Count == ComparableCards)
                    {
                        break;
                    }
                }

                return new BestHand(HandRank.HighCard, bestCards);
            }
        }

        private IList<Rank> GetTypesWithNCards(int[] cardTypeCounts, int n)
        {
            var pairs = new List<Rank>();
            for (var i = cardTypeCounts.Length - 1; i >= 0; i--)
            {
                if (cardTypeCounts[i] == n)
                {
                    pairs.Add((Rank)i);
                }
            }

            return pairs;
        }

        private ICollection<Rank> GetStraightFlushCards(int[] cardSuitCounts, IEnumerable<Card> cards)
        {
            var straightFlushCardTypes = new List<Rank>();
            for (var i = 0; i < cardSuitCounts.Length; i++)
            {
                if (cardSuitCounts[i] < ComparableCards)
                {
                    continue;
                }

                var cardTypeCounts = new int[(int)Rank.Ace + 1];
                foreach (var card in cards)
                {
                    if (card.Suit == (Suit)i)
                    {
                        cardTypeCounts[(int)card.Rank]++;
                    }
                }

                var bestStraight = this.GetStraightCards(cardTypeCounts);
                if (bestStraight != null)
                {
                    straightFlushCardTypes.AddRange(bestStraight);
                }
            }

            return straightFlushCardTypes;
        }

        private ICollection<Rank> GetStraightCards(int[] cardTypeCounts)
        {
            var lastCardType = cardTypeCounts.Length;
            var straightLength = 0;
            for (var i = cardTypeCounts.Length - 1; i >= 1; i--)
            {
                var hasCardsOfType = cardTypeCounts[i] > 0 || (i == 1 && cardTypeCounts[(int)Rank.Ace] > 0);
                if (hasCardsOfType && i == lastCardType - 1)
                {
                    straightLength++;
                    if (straightLength == ComparableCards)
                    {
                        var bestStraight = new List<Rank>(ComparableCards);
                        for (var j = i; j <= i + ComparableCards - 1; j++)
                        {
                            if (j == 1)
                            {
                                bestStraight.Add(Rank.Ace);
                            }
                            else
                            {
                                bestStraight.Add((Rank)j);
                            }
                        }

                        return bestStraight;
                    }
                }
                else
                {
                    straightLength = 0;
                }

                lastCardType = i;
            }

            return null;
        }
    }
}
