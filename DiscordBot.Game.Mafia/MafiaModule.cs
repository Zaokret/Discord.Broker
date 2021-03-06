﻿using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Core.Attributes;
using DiscordBot.Game.Mafia.Attributes;
using DiscordBot.Game.Mafia.Models;
using DiscordBot.Game.Mafia.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia
{
    public class MafiaModule : ModuleBase<SocketCommandContext>
    {
        private readonly MafiaService _game;
        private readonly DiscordSocketClient _client;
        private readonly PollService _poll;
        private readonly CoinService _coin;

        public MafiaModule(MafiaService service, DiscordSocketClient client, PollService poll, CoinService coin)
        {
            _poll = poll ?? throw new ArgumentNullException(nameof(poll));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _game = service ?? throw new ArgumentNullException(nameof(service));
            _coin = coin ?? throw new ArgumentNullException(nameof(coin));
        }

        private async Task<bool> CanPayCostOfEntry(ulong userId)
        {
            int coins = await _coin.GetFundsByUserId(userId);
            return coins >= PriceConfiguration.CostOfEntry;
        }

        [Command("occult")]
        [Summary("Creates a game lobby with one player inside.")]
        public async Task CreatePendingGame()
        {
            if(!(await CanPayCostOfEntry(Context.User.Id)))
            {
                await ReplyAsync(ErrorView.NotEnoughFunds());
            }
            else if(PendingGameService.PendingGames.Any())
            {
                await ReplyAsync(ErrorView.MultipleGames());
            }
            else
            {
                PendingGame game = new PendingGame(Context.User);
                PendingGameService.PendingGames.Add(game);
                await ReplyAsync(InfoView.LobbyCreated(game.Id));
            }
        }
        
        [Command("join")]
        [Summary("Joins a pending warewolf game and initialises when the last player joins.")]
        public async Task JoinPendingGame()
        {
            if (!(await CanPayCostOfEntry(Context.User.Id)))
            {
                await ReplyAsync(ErrorView.NotEnoughFunds());
            }
            else if (PendingGameService.PendingGames.Count == 0)
            {
                await ReplyAsync(ErrorView.NotFound());
            }
            else if(PendingGameService.PendingGames.Any(g => g.Users.Any(u => u.Id == Context.User.Id)))
            {
                await ReplyAsync(ErrorView.AlreadyInLobby());
            }
            else
            {
                PendingGame game = PendingGameService.PendingGames.FirstOrDefault();
                if(game.Active)
                {
                    await ReplyAsync(ErrorView.InProgress());
                }
                else
                {
                    game.Users.Add(Context.User);
                    if (game.Users.Count == GameConfiguration.NumberOfPlayers)
                    {
                        game.Active = true;
                        await ReplyAsync(InfoView.GameStarting());
                        await _game.InitialiseGame(Context, game);
                    }
                    else
                    {
                        await ReplyAsync(InfoView.LobbyStatus(game.Users.Count));
                    }
                }
            }
        }

        [Command("lobby")]
        public async Task PlayersInGameLobby()
        {
            if (PendingGameService.PendingGames.Count == 0)
            {
                await ReplyAsync(ErrorView.NotFound());
            }
            else
            {
                PendingGame game = PendingGameService.PendingGames.FirstOrDefault();
                if(game.Users.Any())
                {
                    string message = string.Join(", ", game.Users.Select(u => MentionUtils.MentionUser(u.Id)));
                    await ReplyAsync(message);
                }
                else
                {
                    await ReplyAsync("Game lobby is empty.");
                }
            }
        }

        [Command("leave")]
        [Summary("Leaves a pending warewolf game.")]
        public async Task LeavePendingGame()
        {
            if (PendingGameService.PendingGames.Count == 0)
            {
                await ReplyAsync(ErrorView.NotFound());
            }
            else
            {
                PendingGame game = PendingGameService.PendingGames.FirstOrDefault();
                IUser user = game.Users.FirstOrDefault(u => u.Id == Context.User.Id);
                if (game.Active)
                {
                    _game.RemoveUserFromPlay(user);
                    await _game.NotifyPlayerLeft(Context.User);
                }
                else
                {
                    game.Users.Remove(user);
                    await ReplyAsync(InfoView.LeftLobby());
                    if (game.Users.Count == 0)
                    {
                        PendingGameService.PendingGames.Clear();
                        await ReplyAsync(InfoView.DeletedLobby());
                    }
                }
            }
        }

        [Command("role")]
        [Summary("Creates a poll for the command issuer or provided user with game role options.")]
        public async Task PollGameRole(IUser user)
        {
            user = user ?? Context.User;
            Poll rolePoll = _poll.CreatePoll(GameElement.Poll.Role(user.Username), GameElement.GetRoleNames().ToList(), Context.User);
            IUserMessage message = await ReplyAsync(string.Empty, false, rolePoll.Message);
            await message.AddReactionsAsync(rolePoll.Emojis.Select(e => new Emoji(e)).ToArray());
        }

        [RequiredGamePlayerAttribute]
        [Command("excommunicate")]
        [Summary("All players vote to kick one player that they suspect is a part of the informed minority.")]
        public async Task VoteToExcommunicate(IUser user)
        {
            if (user != null && _game.IsCommandValid("excommunicate", Context.Channel.Id))
            {
                if(!_game.IsUserPlaying(user.Id))
                {
                    await ReplyAsync(ErrorView.PlayerNotPlaying(user.Username));
                }
                else if (!_game.IsUserAlive(user.Id))
                {
                    await ReplyAsync(ErrorView.PlayerAlreadyInactive(user.Username));
                }
                else
                {
                    await _game.StartExcommunicatePoll(target: user, author: Context.User);
                }
            }
        }

        [RequiredGamePlayerAttribute]
        [Command("sacrifice")]
        [Summary("Informed minority group votes to kill one member of the uninformed majority.")]
        public async Task VoteToSacrifice(IUser user)
        {
            if (user != null &&_game.IsCommandValid("sacrifice", Context.Channel.Id))
            {
                if (!_game.IsUserPlaying(user.Id))
                {
                    await ReplyAsync(ErrorView.PlayerNotPlaying(user.Username));
                }
                else if (!_game.IsUserInTeam(user.Id, GroupType.Uninformed))
                {
                    await ReplyAsync(ErrorView.InvalidSacrificeTarget(user.Username));
                }
                else if(!_game.IsUserAlive(user.Id))
                {
                    await ReplyAsync(ErrorView.RepeatedSacrifice(user.Username));
                } 
                else
                {
                    await _game.StartSacrificePoll(target: user, author: Context.User);
                }
            }
        }

        [RequiredCurrentUser]
        [Command("signs")]
        [Summary("Internal command checking players role and starting the day phase.")]
        public async Task CheckRole(IUser user = null)
        {
            if (_game.IsCommandValid("signs", Context.Channel.Id))
            {
                if (user == null || _game.IsUserPlaying(user.Id))
                {
                    await _game.ResolveVisionPhase(user);
                    await _game.StartDayPhase();
                    await _game.StartPhaseCounter(PhaseType.Day);
                }
                else
                {
                    await ReplyAsync(ErrorView.PlayerNotPlaying(user.Username));
                }
            }
        }

        [RequiredCurrentUser]
        [Command("start")]
        [Summary("Internal command to starts the game once everyone reacts that they are ready.")]
        public async Task StartAfterReady()
        {
            if (_game.IsCommandValid("start", Context.Channel.Id))
            {
                await _game.StartGame();
            }
        }

        [RequiredCurrentUser]
        [Command("kick")]
        [Summary("Internal command for removing players at the end of the day phase and starting the night phase.")]
        public async Task KickPlayer(IUser user = null)
        {
            if (_game.IsCommandValid("kick", Context.Channel.Id))
            {
                await _game.ResolveDayPhase(user);
                await _game.StartNightPhase();
                await _game.StartPhaseCounter(PhaseType.Night);
            }
        }

        [RequiredCurrentUser]
        [Command("kill")]
        [Summary("Internal command for killing players at the end of the night cycle and starting the day vision phase.")]
        public async Task KillPlayer(IUser user = null)
        {
            if (_game.IsCommandValid("kill", Context.Channel.Id))
            {
                await _game.ResolveNightPhase(user);
                await _game.StartVisionPhase();
                await _game.StartPhaseCounter(PhaseType.Investigation);
            }
        }
    }
}
