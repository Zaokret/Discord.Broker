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
    public class MafiaModule : ModuleBase<SocketCommandContext>
    {
        private static List<PendingGame> PendingGames;

        [Command("werewolfs")]
        [Summary("Creates a pending warewolf game.")]
        public async Task CreatePendingGame()
        {
            if (PendingGames == null)
                PendingGames = new List<PendingGame>();
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
            if (PendingGames == null || PendingGames.All(g => g.Id != gameId))
            {
                await ReplyAsync("Game not found.");
            }
            else
            {
                PendingGame game = PendingGames.FirstOrDefault(g => g.Id == gameId);
                if(game.Active)
                {
                    await ReplyAsync("You left the game.");
                }
                else
                {
                    game.Users.Remove(Context.User);
                    await ReplyAsync("You left the game lobby.");
                }
            }
        }

        [Command("join")]
        [Summary("Joins a pending warewolf game.")]
        public async Task JoinPendingGame(string gameId)
        {
            if (PendingGames == null || PendingGames.All(g => g.Id != gameId))
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
                        // start
                        game.Active = true;
                        await ReplyAsync($"Game started.");

                        // game loop

                        // end
                        PendingGames.Remove(game);
                    }
                    else
                    {
                        await ReplyAsync($"{8 - game.Users.Count} more players need to join to start the game.");
                    }
                }
            }
        }
    }
}
