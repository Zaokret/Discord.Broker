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

namespace DiscordBot.Game.CoinWar
{
    public class GameService
    {
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
                game.Players = game.Players.Select(p => p.NextRound()).ToList();
                await Task.WhenAll(
                  game.Players.Select(p =>
                    p.User.SendMessageAsync(PlayerMessage.RoundWin(winningTeam.Value.TeamId))));
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
            SocketMessage[] messages = await Task.WhenAll(
              game.Players.Select((player) =>
                _module.NextMessageAsync(new EnsureFromUserCriterion(player.User.Id))));

            game.Players = await Task.WhenAll(
              game.Players.Select((player, index) =>
                UpdatePlayerBet(game, messages[index], 2)));

            IOrderedEnumerable<Team> teams = game.Players
                .GroupBy(p => p.TeamId)
                .Select(grouping => new Team(grouping))
                .OrderByDescending(team => team.CurrentBet);

            Team winningTeam = teams.First();
            if (teams.Where(t => t.TeamId != winningTeam.TeamId).Any(t => t.CurrentBet == winningTeam.CurrentBet))
                return Optional<Team>.Unspecified;

            return winningTeam;
        }

        private async Task<Player> UpdatePlayerBet(GameObject game, SocketMessage message, int retries)
        {
            Player player = game.Players.First(p => p.User.Id == message.Author.Id);
            if (retries == 0)
            {
                return player.Bet(0);
            }
            Nullable<int> betAmount = ParseMessageToInt(message);
            if (!betAmount.HasValue)
            {
                await player.User.SendMessageAsync(PlayerMessage.BetInfo(game.GameCoins));
                SocketMessage nextBetAttempt = await _module.NextMessageAsync(new EnsureFromUserCriterion(player.User.Id));
                return await UpdatePlayerBet(game, nextBetAttempt, retries - 1);
            }

            if (!player.IsBetValid(betAmount.Value))
            {
                string info = betAmount.Value == 0
                  ? PlayerMessage.BetZero()
                  : PlayerMessage.BetOutsideBudget(player.Coins - player.CurrentBet);

                await player.User.SendMessageAsync(info);
                SocketMessage nextBetAttempt = await _module.NextMessageAsync(new EnsureFromUserCriterion(player.User.Id));
                return await UpdatePlayerBet(game, nextBetAttempt, retries - 1);
            }

            return player.Bet(betAmount.Value);
        }

        private Nullable<int> ParseMessageToInt(SocketMessage message)
        {
            if (!int.TryParse(message.Content, out int bet))
                return null;
            return bet;
        }

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

        private string EndOfGameMsg(Player player)
        {
            return player.Winner
              ? PlayerMessage.GameWin()
              : PlayerMessage.GameLose();
        }
    }
}
