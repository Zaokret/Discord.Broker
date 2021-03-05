using DiscordBot.Poker.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Poker.Models
{
    public class BestHand : IComparable<BestHand>
    {
        internal BestHand(HandRank rankType, ICollection<Rank> cards)
        {
            if (cards.Count != 5)
            {
                throw new ArgumentException("Cards collection should contains exactly 5 elements", nameof(cards));
            }

            this.Cards = cards;
            this.RankType = rankType;
        }

        // When comparing or ranking cards, the suit doesn't matter
        public ICollection<Rank> Cards { get; }

        public HandRank RankType { get; }

        public int CompareTo(BestHand other)
        {
            if (this.RankType > other.RankType)
            {
                return 1;
            }

            if (this.RankType < other.RankType)
            {
                return -1;
            }

            switch (this.RankType)
            {
                case HandRank.HighCard:
                    return CompareTwoHandsWithHighCard(this.Cards, other.Cards);
                case HandRank.Pair:
                    return CompareTwoHandsWithPair(this.Cards, other.Cards);
                case HandRank.TwoPairs:
                    return CompareTwoHandsWithTwoPairs(this.Cards, other.Cards);
                case HandRank.ThreeOfAKind:
                    return CompareTwoHandsWithThreeOfAKind(this.Cards, other.Cards);
                case HandRank.Straight:
                    return CompareTwoHandsWithStraight(this.Cards, other.Cards);
                case HandRank.Flush:
                    return CompareTwoHandsWithHighCard(this.Cards, other.Cards);
                case HandRank.FullHouse:
                    return CompareTwoHandsWithFullHouse(this.Cards, other.Cards);
                case HandRank.FourOfAKind:
                    return CompareTwoHandsWithFourOfAKind(this.Cards, other.Cards);
                case HandRank.StraightFlush:
                    return CompareTwoHandsWithStraight(this.Cards, other.Cards);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int CompareTwoHandsWithHighCard(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstSorted = firstHand.OrderByDescending(x => x).ToList();
            var secondSorted = secondHand.OrderByDescending(x => x).ToList();
            var cardsToCompare = Math.Min(firstHand.Count, secondHand.Count);
            for (var i = 0; i < cardsToCompare; i++)
            {
                if (firstSorted[i] > secondSorted[i])
                {
                    return 1;
                }

                if (firstSorted[i] < secondSorted[i])
                {
                    return -1;
                }
            }

            return 0;
        }

        private static int CompareTwoHandsWithPair(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstPairType = firstHand.GroupBy(x => x).First(x => x.Count() >= 2);
            var secondPairType = secondHand.GroupBy(x => x).First(x => x.Count() >= 2);

            if (firstPairType.Key > secondPairType.Key)
            {
                return 1;
            }

            if (firstPairType.Key < secondPairType.Key)
            {
                return -1;
            }

            // Equal pair => compare high card
            return CompareTwoHandsWithHighCard(firstHand, secondHand);
        }

        private static int CompareTwoHandsWithTwoPairs(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstPairType = firstHand.GroupBy(x => x).Where(x => x.Count() == 2).OrderByDescending(x => x.Key).ToList();
            var secondPairType = secondHand.GroupBy(x => x).Where(x => x.Count() == 2).OrderByDescending(x => x.Key).ToList();

            for (int i = 0; i < firstPairType.Count; i++)
            {
                if (firstPairType[i].Key > secondPairType[i].Key)
                {
                    return 1;
                }

                if (secondPairType[i].Key > firstPairType[i].Key)
                {
                    return -1;
                }
            }

            // Equal pairs => compare high card
            return CompareTwoHandsWithHighCard(firstHand, secondHand);
        }

        private static int CompareTwoHandsWithThreeOfAKind(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstThreeOfAKindType = firstHand.GroupBy(x => x).Where(x => x.Count() == 3).OrderByDescending(x => x.Key).FirstOrDefault();
            var secondThreeOfAKindType = secondHand.GroupBy(x => x).Where(x => x.Count() == 3).OrderByDescending(x => x.Key).FirstOrDefault();
            if (firstThreeOfAKindType.Key > secondThreeOfAKindType.Key)
            {
                return 1;
            }

            if (secondThreeOfAKindType.Key > firstThreeOfAKindType.Key)
            {
                return -1;
            }

            // Equal triples => compare high card
            return CompareTwoHandsWithHighCard(firstHand, secondHand);
        }

        private static int CompareTwoHandsWithStraight(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstBiggestCardType = firstHand.Max();
            if (firstBiggestCardType == Rank.Ace && firstHand.Contains(Rank.Five))
            {
                firstBiggestCardType = Rank.Five;
            }

            var secondBiggestCardType = secondHand.Max();
            if (secondBiggestCardType == Rank.Ace && secondHand.Contains(Rank.Five))
            {
                secondBiggestCardType = Rank.Five;
            }

            return firstBiggestCardType.CompareTo(secondBiggestCardType);
        }

        private static int CompareTwoHandsWithFullHouse(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstThreeOfAKindType = firstHand.GroupBy(x => x).Where(x => x.Count() == 3).OrderByDescending(x => x.Key).FirstOrDefault();
            var secondThreeOfAKindType = secondHand.GroupBy(x => x).Where(x => x.Count() == 3).OrderByDescending(x => x.Key).FirstOrDefault();

            if (firstThreeOfAKindType.Key > secondThreeOfAKindType.Key)
            {
                return 1;
            }

            if (secondThreeOfAKindType.Key > firstThreeOfAKindType.Key)
            {
                return -1;
            }

            var firstPairType = firstHand.GroupBy(x => x).First(x => x.Count() == 2);
            var secondPairType = secondHand.GroupBy(x => x).First(x => x.Count() == 2);
            return firstPairType.Key.CompareTo(secondPairType.Key);
        }

        private static int CompareTwoHandsWithFourOfAKind(
            ICollection<Rank> firstHand,
            ICollection<Rank> secondHand)
        {
            var firstFourOfAKingType = firstHand.GroupBy(x => x).First(x => x.Count() == 4);
            var secondFourOfAKindType = secondHand.GroupBy(x => x).First(x => x.Count() == 4);

            if (firstFourOfAKingType.Key > secondFourOfAKindType.Key)
            {
                return 1;
            }

            if (firstFourOfAKingType.Key < secondFourOfAKindType.Key)
            {
                return -1;
            }

            // Equal pair => compare high card
            return CompareTwoHandsWithHighCard(firstHand, secondHand);
        }
    }
}
