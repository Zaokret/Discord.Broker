using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Poker
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalConfiguration _config;
        private readonly GameService _service;
        private readonly DiscordSocketClient _client;

        public GameModule(GlobalConfiguration config, GameService service, DiscordSocketClient client)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Command("poker-create")]
        [Summary("Creates a poker lobby.")]
        public async Task CreatePokerLobby(int buyIn)
        {
            RestUserMessage message = await Context.Channel.SendMessageAsync("test");
            ulong messageId = message.Id;
            await message.AddReactionAsync(Emote.Parse("JOIN"));
            await _service.DelayedStart(Context.Channel, messageId, buyIn);
        }

        [Command("poker-leave")]
        [Summary("Leave a poker game.")]
        public async Task LeaveGame()
        {
            _service.Leave(Context.User);
            // notify?
        }

        /* ROUND ACTIONS - check if user is part of the game for all first and it's his turn to play */

        [Command("poker-fold")]
        public async Task Fold()
        {
            if(_service.IsUsersTurnToPlay(Context.User.Id))
            {
                await _service.Fold();
            }
            else
            {
                await ReplyAsync("Invalid command. Not your turn to play.");
            }
        }

        [Command("poker-check")]
        public async Task Check()
        {
            if (_service.IsUsersTurnToPlay(Context.User.Id))
            {
                if (!_service.IsBidRaised())
                {
                    await _service.Check();
                }
                else
                {
                    await ReplyAsync("Invalid command. Can't check if bid was raised. Try $poker-call");
                }
            }
            else
            {
                await ReplyAsync("Invalid command. Not your turn to play.");
            }
        }

        [Command("poker-call")]
        public async Task Call ()
        {
            if (_service.IsUsersTurnToPlay(Context.User.Id))
            {
                if (_service.IsBidRaised())
                {
                    if (_service.HasFundsForACall())
                    {
                        await _service.Call();
                    }
                    else
                    {
                        await ReplyAsync("Invalid command. You can only all in because you are out of coins.");
                    }
                }
                else
                {
                    await ReplyAsync("Invalid command. Can't call if bid wasn't raised. Try $poker-check.");
                }
            }
            else
            {
                await ReplyAsync("Invalid command. Not your turn to play.");
            }
        }

        [Command("poker-raise")]
        public async Task Raise(int amount)
        {
            if (_service.IsUsersTurnToPlay(Context.User.Id))
            {
                if (_service.IsValidRaise(amount))
                {
                    if (_service.HasFundsForARaise(amount))
                    {
                        await _service.Raise(amount);
                    }
                    else
                    {
                        await ReplyAsync("Invalid command. You don't have that kind of money.");
                    }
                }
                else
                {
                    await ReplyAsync("Not a valid raise");
                }
            }
            else
            {
                await ReplyAsync("Invalid command. Not your turn to play.");
            }
        }

        [Command("poker-all-in")]
        public async Task AllIn()
        {
            if (_service.IsUsersTurnToPlay(Context.User.Id))
            {
                // they can't all in with a check or a raise automatically, only by calling this command
                await _service.AllIn();
            }
            else
            {
                await ReplyAsync("Invalid command. Not your turn to play.");
            }
        }
    }
}
