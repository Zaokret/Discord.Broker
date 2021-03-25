using Discord;
using DiscordBot.Models;
using DiscordBot.Poker.Enums;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Poker.Models
{
    public class Game
    {
        public Game(IEnumerable<IUser> users, int buyIn)
        {
            Players = users.Select((user, index) => new Player
            {
                Bet = new Wallet(0),
                Wallet = new Wallet(buyIn),
                HasButton = users.Count() > 2 ? index == 3 : index == 1,
                Hole = new List<Card>(),
                UserId = user.Id,
                Username = user.Username
            }).ToList();

            var turn = new Turn(new Queue<Player>(Players));
            turn.PutAtBeginingOfQue(Players.FirstOrDefault(p => p.HasButton).UserId);

            Hand = new Hand(turn.Players);
        }

        public State State { get; set; }
        public List<Player> Players { get; set; }

        public Hand Hand { get; set; }

        public bool IsOver()
        {
            return Hand.IsOver() && Hand.Players.Count < 2;
        }

        public void NextHand()
        {
            // take only players who have funds left
            var players = Hand.Players.Where(p => {
                var player = Players.FirstOrDefault(player => player.UserId == p.UserId);
                return player != null && player.Wallet.Funds > 0;
            });

            // take players from the last hand
            var turn = new Turn(new Queue<Player>(players));

            // put old button at the start
            turn.PutAtBeginingOfQue(Players.FirstOrDefault(p => p.HasButton).UserId);

            // move it once space
            turn.Next();
            Player newButton = Hand.Playing();

            // save new button and empty their bets for the new hand 
            Players = turn.Players.Select(p =>
            {
                p.HasButton = p.UserId == newButton.UserId;
                p.Bet.Empty();
                p.Hole = new List<Card>();
                return p;
            }).ToList();

            Hand = new Hand(new Queue<Player>(Players));
        }
    }

}
