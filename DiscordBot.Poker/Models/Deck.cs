using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Poker.Enums;

namespace DiscordBot.Poker.Models
{
    public class Deck
    {
        public Deck()
        {
            IEnumerable<Rank> ranks = (IEnumerable<Rank>)Enum.GetValues(typeof(Rank));
            IEnumerable<Suit> suits = (IEnumerable<Suit>)Enum.GetValues(typeof(Suit));

            Cards = suits.SelectMany(suit => ranks.Select(rank => new Card() { Rank = rank, Suit = suit })).ToList();
        }
        public List<Card> Cards { get; set; }

        public void Suffle()
        {
            Cards.Shuffle();
        }

        public IEnumerable<Card> Draw(int num)
        {
            var drawn = Cards.Take(num);
            Cards.RemoveRange(0, num);
            return drawn;
        }
    }

}
