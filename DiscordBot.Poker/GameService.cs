using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using DiscordBot.Broker;
using DiscordBot.Models;
using DiscordBot.Poker.Models;
using Game = DiscordBot.Poker.Models.Game;
using Action = DiscordBot.Poker.Models.Action;
using DiscordBot.Poker.Enums;
using ActionType = DiscordBot.Poker.Enums.ActionType;
using DiscordBot.Poker.Helpers;

/*
         * 2 - 10 player game
         * turn -> hand -> game
         * 
         pre flop ( button that rotates, small-blind, blind, deal hole, turn goes on until everyone has played or called the current raise bet )
         flop ( deal 3 community cards, turn goes on )
         turn ( deal 1 community card, turn goes on )
         river ( deal 1 community card, turn goes on )
         showdown ( reveal card if there are more then one player in play )
         */

/*
 there are 4 turns to bet
 */

/*
 determining the winner and tie
 rewards with all in caps
 */

/*
 two player rules with blinds
 */

/*
 track where the button is
 track pot
 track players balance
 track current raise bet
 track if all played their turn
 track how many players all in play
 */

/*
 * start hand
 * 
 * preflop: position button and blinds
 * play actions ( call, raise, fold )
 * 
 */

/*
    create lobby
    join lobby by reacting within a specified timeframe
    start game when timeframe expires if there are enough players

    send players their hands
    show community and actions
    * act with reactions or messages?
    after each has acted reveal another card
    repeat * until 5th card is revealed or there are no more moves left to make
    reward players
    game continues until only one player remains with coins or everyone left expect one
*/

namespace DiscordBot.Poker
{
    public class GameService
    {
        private readonly DiscordSocketClient _client;
        private readonly CoinService _coinService;
        private IList<ulong> UserIds { get; set; } = new List<ulong>();
        private Game Game { get; set; }

        

        public async Task DelayedStart(ISocketMessageChannel channel, ulong messageId, int buyIn)
        {
            await Task.Delay(30 * 1000);
            var message = await channel.GetMessageAsync(messageId, CacheMode.AllowDownload);
            var users = await message.GetReactionUsersAsync(Emote.Parse("JOIN"), 100).FlattenAsync();

            if(users.Count() < 3)
            {
                await channel.SendMessageAsync("Not enough players reacted.");
                return;
            }

            List<IUser> boughtIn = new List<IUser>();
            foreach (var user in users)
            {
                if (boughtIn.Count < 11)
                {
                    break;
                }

                float funds = await _coinService.GetFundsByUserId(user.Id);
                if (funds >= buyIn)
                {
                    await _coinService.RemoveFunds(user.Id, buyIn);
                    boughtIn.Add(user);
                }
            }

            if (boughtIn.Count > 2)
            {
                await Start(channel, boughtIn, buyIn);
            }
            else
            {
                await channel.SendMessageAsync("Not enough players with sufficient funds.");
            }
        }
        
        private async Task Start(ISocketMessageChannel channel, IEnumerable<IUser> users, int buyIn)
        {
            Game game = new Game(users, buyIn);

            // send DMs to players
            await Task.WhenAll(users.Select(u => u.SendMessageAsync("test")));

            await Task.Delay(5 * 1000);

            game.State = State.Active;

            // store game into database
            Game = game;

            // play order and next to play and actions
            await channel.SendMessageAsync("test");
        }

        public NextToPlayBreakdown NextToPlay()
        {
            Player p = Game.Hand.Playing();

            List<Action> availableActions = new List<Action>(new[] { new Action() { Name = ActionType.Fold, Amount = 0 } });

            if(!IsBidRaised())
            {
                availableActions.Add(new Action() { Name = ActionType.Check, Amount = 0 });
            }
            else if(HasFundsForACall())
            {
                availableActions.Add(new Action() { Name = ActionType.Call, Amount = (int)(Game.Hand.CurrentBid() - p.Bet.Funds) });
            }

            float minRaise = Game.Hand.MinRaise();
            if (p.Wallet.CanWithdraw(minRaise))
            {
                availableActions.Add(new Action() { Name = ActionType.Raise, Amount = (int)minRaise });
            }

            availableActions.Add(new Action() { Name = ActionType.AllIn, Amount = (int)p.Wallet.Funds });

            return new NextToPlayBreakdown()
            {
                Pot = Game.Hand.Pot,
                AvailableActions = availableActions,
                Player = p,
                Community = Game.Hand.Community
            };
        }

        public Showdown Showdown()
        {
            var hands = HandHelpers.GetPlayerHands(Game.Hand.Players, Game.Hand.Community);
            var winner = hands.FirstOrDefault();
            var tiedPlayers = hands.Count(hand => hand.HandRankValue == winner.HandRankValue);
            if (tiedPlayers > 1)
            {
                // tie
                return new Showdown();
            } 
            else
            {
                Reward reward = new Reward() { 
                    Amount = (int)Game.Hand.Pot.Empty(), 
                    UserId = winner.Player.UserId 
                };
                return new Showdown()
                {
                    Hands = hands,
                    Winner = winner,
                    Rewards = new List<Reward>(new[] { reward })
                };
            }
        }

        public void Next()
        {
            if (!Game.IsOver())
            {
                if (!Game.Hand.IsOver())
                {
                    if (!Game.Hand.Turn.IsOver())
                    {
                        if (Game.Hand.Playing().Wallet.Funds > 0)
                        {
                            var breakdown = NextToPlay();
                            // send breakdown for next to play
                        }
                        else
                        {
                            // skip those that are all in
                            Game.Hand.Turn.Next();
                            Next();
                        }
                    }
                    else
                    {
                        // turn over
                        Game.Hand.NextTurn();
                        Next();
                    }
                }
                else
                {
                    Showdown showdown = Showdown();
                    // notify

                    GiveReward(showdown.Rewards);

                    PlayersToBeRemoved();
                    // notify

                    Game.NextHand();
                    Next();
                }
            }
            else
            {
                // game over
                // terminate
            }
        }

        public IEnumerable<Player> PlayersToBeRemoved()
        {
            return Game.Players.Where(p => p.Wallet.Funds < 1);
        }

        public void GiveReward(IEnumerable<Reward> rewards)
        {
            Game.Hand.Players = Game.Hand.Players.Select(p =>
            {
                var reward = rewards.FirstOrDefault(r => r.UserId == p.UserId);
                if (reward != null)
                {
                    p.Wallet.Deposit(reward.Amount);
                }
                return p;
            }).ToList();
        }

        public bool IsUsersTurnToPlay(ulong userId)
        {
            return Game.Hand.Playing().UserId == userId;
        }

        public bool IsValidRaise(int amount)
        {
            return Game.Hand.IsValidRaise(amount);
        }
        
        public bool HasFundsForACall()
        {
            return Game.Hand.CanCall();
        }

        public bool HasFundsForARaise(int amount)
        {
            return Game.Hand.CanRaise(amount);
        }

        public bool IsBidRaised()
        {
            return !Game.Hand.DidEveryoneCallTheBet();
        }

        public async Task Fold()
        {
            Game.Hand.Fold();
            Next();
        }

        public async Task Check()
        {
            Game.Hand.Check();
            Next();
        }

        public async Task Call()
        {
            Game.Hand.Call();
            Next();
        }

        public async Task Raise(int amount)
        {
            Game.Hand.Raise(amount);
            Next();
        }

        public async Task AllIn()
        {
            Game.Hand.AllIn();
            Next();
        }

        public void Leave(IUser user)
        {
            Game.Hand.Leave(user.Id);
            Game.IsOver();
            // terminate
        }

        public void Save() { }
    }
}
