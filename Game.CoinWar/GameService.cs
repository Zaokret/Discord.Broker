using Discord;
using DiscordBot.Game.CoinWar.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Linq;
using Discord.Addons.Interactive;
using Discord.Commands;
using DiscordBot.Game.CoinWar.Views;
using DiscordBot.Game.CoinWar.Extensions;

namespace DiscordBot.Game.CoinWar
{
    public class GameService
    {
        private static readonly TimeSpan RoundTimeoutTimeSpan = TimeSpan.FromSeconds(15);

        private readonly DiscordSocketClient _client;
        private InteractiveBase<SocketCommandContext> _module { get; set; }
        private static ICollection<PendingGame> PendingGames { get; set; }

        public GameService(DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
        }
        
        public void GiveModuleControl(InteractiveBase<SocketCommandContext> module)
        {
            _module = module;
        }

        public async Task<bool> FindPendingGameAndStartAsync(string gameId, IUser user)
        {
            GuildEmote emote = _client.Guilds.SelectMany(g => g.Emotes).FirstOrDefault(e => e.Name == "Attar_Coin");
            await _module.Context.Message.AddReactionAsync(emote);

            if (_module == null)
                throw new ArgumentNullException($"{nameof(GameService)} requires control of {nameof(InteractiveBase<SocketCommandContext>)} module.");

            if (PendingGames == null)
                return false;

            PendingGame game = PendingGames.FirstOrDefault(g => g.Guid == gameId);
            if (game == null)
                return false;

            PendingGames.Remove(game);
            await SpawnGame(game, user);
            return true;
        }

        private async Task SpawnGame(PendingGame pendingGame, IUser acceptingUser)
        {
            int delayUntilStart = 5;
            IUser userInitiator = await _client.Rest.GetUserAsync(pendingGame.UserInitiatorId);

            await Task.WhenAll(new[]
            {
                userInitiator.SendMessageAsync(PlayerMessage.GameStart(acceptingUser.Username, delayUntilStart)),
                acceptingUser.SendMessageAsync(PlayerMessage.GameStart(userInitiator.Username, delayUntilStart))
            });

            await Task.Delay(delayUntilStart * 1000);
            await StartGame(pendingGame, userInitiator, acceptingUser);
        }

        private async Task StartGame(PendingGame pendingGame, IUser userOne, IUser userTwo)
        {
            GameObject game = new GameObject(pendingGame, userOne, userTwo);

            for (int currentRound = 1; currentRound <= game.NumberOfRounds; currentRound++)
            {
                await Task.WhenAll(
                        game.Players.Select((player) =>
                            player.User.SendMessageAsync(PlayerMessage.RoundStart(currentRound, player.Coins))));

                Optional<Round> fastRound = ShortCircuitRound(game);
                if (fastRound.IsSpecified)
                {
                    game.Rounds.Add(fastRound.Value);
                    continue;
                }

                Round round = await ExecuteRound(game, currentRound);
                game.Rounds.Add(round);
                if (game.Rounds.Count(r => r.WinnerTeamId == round.WinnerTeamId) == game.RoundsToVictory)
                {
                    game.EndWithWinner(round.WinnerTeamId);
                    break;
                }
            }

            if (!game.WinnerFound)
            {
                GameScore winnerScore = game.Rounds
                  .Where(r => !r.BothTeamLost)
                  .GroupBy(r => r.WinnerTeamId)
                  .Select(g => new GameScore(g.Key, g.Count()))
                  .OrderByDescending(s => s.RoundsWon)
                  .FirstOrDefault();

                game.EndWithWinner(winnerScore.TeamId);
            }

            await Task.WhenAll(
                game.Players.Select((player) =>
                  player.User.SendMessageAsync(EndOfGameMsg(player))));
        }

        private Optional<Round> ShortCircuitRound(GameObject game)
        {
            IEnumerable<Team> teams = game.Players
                .GroupBy(p => p.TeamId)
                .Select(grouping => new Team(grouping));

            if (teams.All(t => t.CoinsLeft == 0))
            {
                return Round.BothLost();
            }
            Team losingTeam = teams.FirstOrDefault(t => t.CoinsLeft == 0);
            if (losingTeam != null)
                return new Round(losingTeam.TeamId);

            return Optional<Round>.Unspecified;
        }

        private async Task<Round> ExecuteRound(GameObject game, int round)
        {
            Optional<Team> winningTeam = await EvaluateWinnerOrWar(game, round);
            if (winningTeam.IsSpecified)
            {
                // create round score
                Player winner = game.Players.FirstOrDefault(p => p.TeamId == winningTeam.Value.TeamId);
                Player loser = game.Players.FirstOrDefault(p => p.TeamId != winningTeam.Value.TeamId);
                Func<Player, Embed> score = RoundView.Of(round, winner, loser);

                // message players
                await Task.WhenAll(
                  game.Players.Select(p =>
                    p.User.SendMessageAsync(score(p))));

                // logic
                game.Players = game.Players.Select(p => p.NextRound()).ToList();
                return new Round(winningTeam.Value.TeamId);
            }
            else
            {
                await Task.WhenAll(
                  game.Players.Select(p =>
                    p.User.SendMessageAsync(PlayerMessage.War())));
                return await ExecuteRound(game, round);
            }
        }

        private async Task<Optional<Team>> EvaluateWinnerOrWar(GameObject game, int round)
        {
            SocketMessage[] nullableMessages = await Task.WhenAll(
              game.Players.Select(ReadPlayerMessage));

            IEnumerable<Optional<SocketMessage>> messages = nullableMessages.Select(Try);

            game.Players = await Task.WhenAll(
              game.Players.Select((player, index) =>
              {
                  Optional<SocketMessage> message = messages.FirstOrDefault(m => m.IsSpecified && m.Value.Author.Id == player.User.Id);
                  return UpdatePlayerBet(game, player, message, retries: 3);
              }));

            IOrderedEnumerable<Team> teams = game.Players
                .GroupBy(p => p.TeamId)
                .Select(grouping => new Team(grouping))
                .OrderByDescending(team => team.CurrentBet);

            Team winningTeam = teams.First();

            // Is tied with another team
            if (teams.Where(t => t.TeamId != winningTeam.TeamId).Any(t => t.CurrentBet == winningTeam.CurrentBet))
                return Optional<Team>.Unspecified;

            return winningTeam;
        }

        // TODO Consolidate retries
        private async Task<Player> UpdatePlayerBet(GameObject game, Player player, Optional<SocketMessage> message, int retries)
        {
            if (retries == 0)
            {
                return player.Bet(0);
            }

            // Retry on timeout
            if(!message.IsSpecified)
            {
                await player.User.SendMessageAsync(PlayerMessage.RoundTimeout(retries - 1));
                Optional<SocketMessage> nextBetAttempt = await TryAsync(ReadPlayerMessage(player));
                return await UpdatePlayerBet(game, player, nextBetAttempt, retries - 1);
            }

            Nullable<int> betAmount = ParseMessageToInt(message.Value);

            // Retry on message not being a number
            if (!betAmount.HasValue)
            {
                await player.User.SendMessageAsync(PlayerMessage.BetInfo(game.GameCoins));
                Optional<SocketMessage> nextBetAttempt = await TryAsync(ReadPlayerMessage(player));
                return await UpdatePlayerBet(game, player, nextBetAttempt, retries - 1);
            }

            // Retry on invalid bet
            if (!player.IsBetValid(betAmount.Value))
            {
                string info = betAmount.Value == 0
                  ? PlayerMessage.BetZero()
                  : PlayerMessage.BetOutsideBudget(player.Coins - player.CurrentBet);

                await player.User.SendMessageAsync(info);
                Optional<SocketMessage> nextBetAttempt = await TryAsync(ReadPlayerMessage(player));
                return await UpdatePlayerBet(game, player, nextBetAttempt, retries - 1);
            }

            return player.Bet(betAmount.Value);
        }

        // Bet helpers

        private Nullable<int> ParseMessageToInt(SocketMessage message)
        {
            if (!int.TryParse(message.Content, out int bet))
                return null;
            return bet;
        }

        // Pending games

        private void RemoveGamesForUser(ulong userId)
        {
            if (PendingGames != null)
            {
                foreach (PendingGame existingGame in PendingGames.Where(g => g.UserInitiatorId == userId))
                {
                    PendingGames.Remove(existingGame);
                }
            }
        }

        public string CreatePendingGame(ulong userId)
        {
            RemoveGamesForUser(userId);
            if (PendingGames == null)
                PendingGames = new List<PendingGame>();

            PendingGame game = new PendingGame(userId);
            PendingGames.Add(game);
            return game.Guid;
        }

        // Message helpers

        private string EndOfGameMsg(Player player)
        {
            return player.Winner
              ? PlayerMessage.GameWin()
              : PlayerMessage.GameLose();
        }

        // Life quality helpers

        private Optional<T> Try<T>(T obj)
        {
            if (obj == null)
                return Optional<T>.Unspecified;
            return obj;
        }

        private Task<SocketMessage> ReadPlayerMessage(Player player)
        {
            return _module.NextMessageAsync(new EnsureFromUserCriterion(player.User.Id), RoundTimeoutTimeSpan);
        }

        private async Task<Optional<T>> TryAsync<T>(Task<T> task)
        {
            return Try(await task);
        }
    }
}
