using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Broker;
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

        
        [Command("dividerules")]
        public async Task SendRules()
        {
            foreach (Embed message in GameRules.Of())
            {
                await ReplyAsync(string.Empty, false, message);
            }
        }

        [Command("divide")]
        [Summary("Creates a pending warewolf game.")]
        public async Task CreatePendingGame()
        {
            if(!(await CanPayCostOfEntry(Context.User.Id)))
            {
                await ReplyAsync(ErrorView.NotEnoughFunds());
            }
            else if(PendingGameService.PendingGames.Any(g => g.Active))
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
        public async Task JoinPendingGame(string gameId)
        {
            if (!(await CanPayCostOfEntry(Context.User.Id)))
            {
                await ReplyAsync(ErrorView.NotEnoughFunds());
            }
            else if (PendingGameService.PendingGames.All(g => g.Id != gameId))
            {
                await ReplyAsync(ErrorView.NotFound());
            }
            else if(PendingGameService.PendingGames.Any(g => g.Id == gameId && 
                DateTime.Now.Subtract(g.CreatedAt).Minutes <= PendingGameService.ExpirationInMinutes))
            {
                PendingGameService.PendingGames.Remove(PendingGameService.PendingGames.FirstOrDefault(g => g.Id == gameId));
                await ReplyAsync(ErrorView.GameExpired(PendingGameService.ExpirationInMinutes));
            }
            else
            {
                PendingGame game = PendingGameService.PendingGames.FirstOrDefault(g => g.Id == gameId);
                if(game.Active)
                {
                    await ReplyAsync(ErrorView.InProgress());
                }
                else
                {
                    game.Users.Add(Context.User);
                    if (game.Users.Count == 8)
                    {
                        game.Active = true;
                        await _game.InitialiseGame(Context, game);
                    }
                    else
                    {
                        await ReplyAsync(InfoView.LobbyStatus(game.Users.Count));
                    }
                }
            }
        }

        [Command("leave")]
        [Summary("Leaves a pending warewolf game.")]
        public async Task LeavePendingGame(string gameId)
        {
            if (PendingGameService.PendingGames.All(g => g.Id != gameId))
            {
                await ReplyAsync(ErrorView.NotFound());
            }
            else
            {
                PendingGame game = PendingGameService.PendingGames.FirstOrDefault(g => g.Id == gameId);
                if (game.Active)
                {
                    _game.RemoveUserFromPlay(Context.User);
                    await _game.NotifyPlayerLeft(Context.User);
                }
                else
                {
                    game.Users.Remove(Context.User);
                    await ReplyAsync(InfoView.LeftLobby());
                }
            }
        }

        [Command("role")]
        [Summary("Creates a poll for the command issuer or provided user with game role options.")]
        public async Task PollGameRole(IUser user)
        {
            user = user ?? Context.User;
            PollCommandArguments args = new PollCommandArguments
            {
                Title = $"What is {user.Username}'s role ?",
                Description = $"Vote for the role you believe {user.Username} has."
            };
            
            Poll rolePoll = _poll.CreatePoll(args, GameElement.GetRoleNames().ToList(), Context.User);
            await ReplyAsync(string.Empty, false, rolePoll.Message);
        }

        [RequiredGameActive]
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
                    await _game.StartSacrificePoll(user);
                }
            }
        }

        [RequiredGameActive]
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
                    await _game.StartSacrificePoll(user);
                }
            }
        }

        [RequiredGameActive]
        [Command("signs")]
        [Summary("Internal command and external command checking players role and starting the day phase.")]
        public async Task CheckRole(IUser user = null)
        {
            if((_game.IsInvestigator(Context.User.Id) || 
                _client.CurrentUser.Id == Context.User.Id)
            && _game.IsPhase(PhaseType.Investigation))
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
