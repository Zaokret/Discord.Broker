using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia
{
    // TODO SPLIT INTO PARTIAL CLASSES
    public class MafiaModule : ModuleBase<SocketCommandContext>
    {
        private static List<PendingGame> PendingGames = new List<PendingGame>();
        private readonly MafiaService _service;
        private readonly DiscordSocketClient _client;

        public MafiaModule(MafiaService service, DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /* TODO REMOVE PENDING GAME WHEN END
           PendingGames.Remove(game);
        */

        // TODO RENAME COMMANDS TO BE GAME UNIQUE

        /* PREPARATION COMMANDS */

        [Command("werewolfs")]
        [Summary("Creates a pending warewolf game.")]
        public async Task CreatePendingGame()
        {
            PendingGame game = new PendingGame(Context.User);
            if(PendingGames.Any(g => g.Active))
            {
                await ReplyAsync("There could be only one active game the a time. Wait until the active group finishes to initiate a game. Message 'JJ 3maj' if you find yourself waiting for your time to play too often.");
            }
            else
            {
                PendingGames.Add(game);
                await ReplyAsync($"Warewolf game was created. 7 more players need to join to start the game. To join they must use command '$join {game.Id}'");
            }
        }

        [Command("leave")]
        [Summary("Leaves a pending warewolf game.")]
        public async Task LeavePendingGame(string gameId)
        {
            if (PendingGames.All(g => g.Id != gameId))
            {
                await ReplyAsync("Game not found.");
            }
            else
            {
                PendingGame game = PendingGames.FirstOrDefault(g => g.Id == gameId);
                if(game.Active)
                {
                    _service.RemoveUserFromPlay(Context.User);
                    await _service.NotifyPlayerLeft(Context.User);
                }
                else
                {
                    game.Users.Remove(Context.User);
                    await ReplyAsync("You left the game lobby.");
                }
            }
        }

        [Command("join")]
        [Summary("Joins a pending warewolf game and initialises when the last player joins.")]
        public async Task JoinPendingGame(string gameId)
        {
            if (PendingGames.All(g => g.Id != gameId))
            {
                await ReplyAsync("Game not found.");
            }
            else if(PendingGames.Any(g => g.Id == gameId && DateTime.Now.Subtract(g.Created).Minutes <= 60))
            {
                PendingGames.Remove(PendingGames.FirstOrDefault(g => g.Id == gameId));
                await ReplyAsync("More than 60 minutes has passed since game was initiated. Initiate again.");
            }
            else
            {
                PendingGame game = PendingGames.FirstOrDefault(g => g.Id == gameId);
                if(game.Active)
                {
                    await ReplyAsync("Game already started.");
                }
                else
                {
                    game.Users.Add(Context.User);
                    if (game.Users.Count == 8)
                    {
                        game.Active = true;
                        await _service.InitialiseGame(Context, game);
                    }
                    else
                    {
                        await ReplyAsync($"{8 - game.Users.Count} more players need to join to start the game.");
                    }
                }
            }
        }

        /* PLAYER */

        private const string Excommunicate = "excommunicate";
        [RequiredGameActive]
        [Command(Excommunicate)]
        [Summary("Day phase voting.")]
        public async Task VoteToExcommunicate(IUser user)
        {
            if (_service.IsCommandValid(Excommunicate, Context.Channel.Id))
            {
                if(!_service.IsUserPlaying(user.Id))
                {
                    await ReplyAsync("Player not in the game.");
                }
                else if (!_service.IsUserAlive(user.Id))
                {
                    await ReplyAsync("You can't vote to excommunicate a player who was already excommunicated or dead.");
                }
                else
                {
                    await _service.StartSacrificePoll(user);
                }
            }
        }

        private const string Sacrifice = "sacrifice";
        [RequiredGameActive]
        [Command(Sacrifice)]
        [Summary("Night phase voting.")]
        public async Task VoteToSacrifice(IUser user)
        {
            if (_service.IsCommandValid(Sacrifice, Context.Channel.Id))
            {
                if (!_service.IsUserPlaying(user.Id))
                {
                    await ReplyAsync("Player not in the game.");
                }
                else if (!_service.IsUserInTeam(user.Id, MafiaService.TeamType.Villager))
                {
                    await ReplyAsync("You can't vote to sacrifice a cultist.");
                }
                else if(!_service.IsUserAlive(user.Id))
                {
                    await ReplyAsync("You can't vote to sacrifice a player who was already sacrificed.");
                } 
                else
                {
                    await _service.StartSacrificePoll(user);
                }
            }
        }

        /* PLAYER AND INTERNAL */

        private const string Visions = "vision";
        [Command(Visions)]
        [Summary("Internal command and external command checking players role and starting the day phase.")]
        public async Task CheckRole(IUser user = null)
        {
            if(_service.IsSeer(Context.User.Id) && _service.IsPhase(MafiaService.Phase.Vision))
            {
                if (!_service.IsUserPlaying(user.Id))
                {
                    await ReplyAsync("Player not in the game.");
                }
                else
                {
                    await _service.ResolveVisionPhase(user);
                    await _service.StartDayPhase();
                    await _service.StartPhaseCounter(MafiaService.Phase.Day);
                }
            }
        }

        /* INTERNAL */

        private const string StartCommand = "start";
        [RequiredCurrentUser]
        [Command(StartCommand)]
        [Summary("Internal command to starts the game once everyone reacts that they are ready.")]
        public async Task StartAfterReady()
        {
            if (_service.IsCommandValid(StartCommand, Context.Channel.Id))
            {
                await _service.StartGame();
            }
        }

        private const string Remove = "remove";
        [RequiredCurrentUser]
        [Command(Remove)]
        [Summary("Internal command for removing players at the end of the day phase and starting the night phase.")]
        public async Task RemovePlayer(IUser user = null)
        {
            if (_service.IsCommandValid(Remove, Context.Channel.Id))
            {
                await _service.ResolveDayPhase(user);
                await _service.StartNightPhase();
                await _service.StartPhaseCounter(MafiaService.Phase.Night);
            }
        }

        private const string Kill = "kill";
        [RequiredCurrentUser]
        [Command(Kill)]
        [Summary("Internal command for killing players at the end of the night cycle and starting the day phase.")]
        public async Task KillPlayer(IUser user = null)
        {
            if (_service.IsCommandValid(Kill, Context.Channel.Id))
            {
                await _service.ResolveNightPhase(user);
                await _service.StartVisionPhase();
                await _service.StartPhaseCounter(MafiaService.Phase.Vision);
            }
        }
    }
}
