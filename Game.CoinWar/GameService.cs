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
using DiscordBot.Broker;
using DiscordBot.Core.Models;
using DiscordBot.Game.CoinWar.Exceptions;

namespace DiscordBot.Game.CoinWar
{
    public class GameService
    {
        private static readonly TimeSpan RoundTimeoutTimeSpan = TimeSpan.FromSeconds(20);
        private static readonly int PendingGameTimeoutMinutes = 30;

        private readonly DiscordSocketClient _client;
        private InteractiveBase<SocketCommandContext> _module { get; set; }
        private static ICollection<PendingGame> PendingGames = new List<PendingGame>();
        private readonly CoinService _coinService;
        private readonly CollectablePickerService _collectablePickerService;

        public void GiveModuleControl(InteractiveBase<SocketCommandContext> module)
        {
            _module = module;
        }

        public GameService(DiscordSocketClient client, CoinService coinService, CollectablePickerService collectablePickerService)
        {
            _coinService = coinService ?? throw new ArgumentNullException(nameof(coinService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _collectablePickerService = collectablePickerService ?? throw new ArgumentNullException(nameof(collectablePickerService));
        }

        #region Initialization

        public async Task CreateOrStartGameAsync(IUser commandIssuerUser, IUser referencedUser, string channelName)
        {
            #region Validation

            if (_module == null)
            {
                throw new ArgumentNullException($"{nameof(GameService)} requires control of {nameof(InteractiveBase<SocketCommandContext>)} module.");
            }

            if (IsUserInActiveGame(commandIssuerUser))
            {
                await commandIssuerUser.SendMessageAsync("You can't play two games at the same time.");
                return;
            }

            if (IsUserInActiveGame(referencedUser))
            {
                await commandIssuerUser.SendMessageAsync($"{referencedUser.Username} is already playing the game with another user.");
                return;
            }

            #endregion

            Optional<PendingGame> pendingGame = TryFindPendingGame(commandIssuerUser, referencedUser);

            if (pendingGame.IsSpecified)
            {
                await StartAsync(pendingGame.Value);
                PendingGames.Remove(pendingGame.Value);
            }
            else
            {
                PendingGame game = CreatePendingGame(commandIssuerUser, referencedUser);
                await Task.WhenAll(new[]
                {
                    commandIssuerUser.SendMessageAsync($"Challange has been sent to {referencedUser.Username}. He must accept before {DateTime.Now.AddMinutes(PendingGameTimeoutMinutes):H:mm}", false, GameView.Info(game)),
                    referencedUser.SendMessageAsync($"{commandIssuerUser.Username} has challanged you to a game. To join send this command in {channelName} channel '$auction {commandIssuerUser.Mention}'", false, GameView.Info(game))
                });
            }
        }

        public PendingGame CreatePendingGame(IUser commandIssuerUser, IUser referencedUser)
        {
            CollectableEntity collectable = _collectablePickerService.GetRandomCollectable();
            PendingGame game = new PendingGame(commandIssuerUser, referencedUser, collectable);
            PendingGames.Add(game);
            return game;
        }

        private Optional<PendingGame> TryFindPendingGame(IUser commandIssuerUser, IUser referencedUser)
        {
            return OptionalUtilities.Try(PendingGames.FirstOrDefault(g =>
                g.Initiator.Id == referencedUser.Id &&
                g.Challanged.Id == commandIssuerUser.Id &&
                DateTime.Now.Subtract(g.CreatedDate).Minutes <= PendingGameTimeoutMinutes));
        }

        private bool IsUserInActiveGame(IUser user)
        {
            if (PendingGames == null)
                return false;

            return PendingGames
                    .Where(g => g.Started)
                    .Any(g => g.Initiator.Id == user.Id || g.Challanged.Id == user.Id);
        }

        #endregion

        #region Game Loop

        private async Task<GameObject> StartAsync(PendingGame pendingGame)
        {
            pendingGame.Started = true;

            await Task.WhenAll(new[]
            {
                pendingGame.Initiator.SendMessageAsync(PlayerMessage.GameStart(pendingGame.Challanged.Username, GameConfiguration.SecondsBeforeStart)),
                pendingGame.Challanged.SendMessageAsync(PlayerMessage.GameStart(pendingGame.Initiator.Username, GameConfiguration.SecondsBeforeStart))
            });

            await Task.Delay(GameConfiguration.SecondsBeforeStart * 1000);

            GameObject game = new GameObject(pendingGame);
            for (int currentRound = 1; currentRound <= game.NumberOfRounds; currentRound++)
            {
                // Short route to victory
                Optional<Round> fastRound = ShortCircuitRound(game);
                if (fastRound.IsSpecified)
                {
                    game.Rounds.Add(fastRound.Value);
                    await ShortCircuitGame(game);
                    break;
                }

                await SendRoundMessage(game, currentRound);
                Round round = await ExecuteRound(game, currentRound);
                game.Rounds.Add(round);

                // Normal route to victory
                if (game.Rounds.Count(r => r.WinnerTeamId == round.WinnerTeamId) == game.RoundsToReward)
                {
                    game.EndWithWinner(round.WinnerTeamId);
                    break;
                }
            }

            Player winner = game.Players.FirstOrDefault(p => p.Winner);
            if (winner != null && game.Rounds.Count(r => r.WinnerTeamId == winner.TeamId) == game.RoundsToReward)
            {
                await _coinService.AddFunds(winner.User.Id, (float)winner.Coins);
                await SendEndOfGameMessage(game);
            }
            else
            {
                await Task.WhenAll(game.Players.Select(p => p.User.SendMessageAsync("Game over")));
            }

            return game;
        }

        #endregion

        #region ShortCircuit

        private Optional<Round> ShortCircuitRound(GameObject game)
        {
            IEnumerable<Team> teams = game.Players
                .GroupBy(p => p.TeamId)
                .Select(grouping => new Team(grouping));

            if (teams.All(t => t.CoinsLeft < GameConfiguration.MinimumBet))
            {
                return Round.BothWithoutFunds();
            }

            Team losingTeam = teams.FirstOrDefault(t => t.CoinsLeft < GameConfiguration.MinimumBet);
            if (losingTeam != null)
            {
                game.Players = game.Players.Select(p =>
                {
                    if (losingTeam.TeamId != p.TeamId)
                        return p.Bet(GameConfiguration.MinimumBet);
                    return p;
                }).ToList();

                Round round = new Round(
                    winner: game.Players.First(p => losingTeam.TeamId != p.TeamId),
                    loser: game.Players.First(p => losingTeam.TeamId == p.TeamId),
                    withoutFunds: true);
                game.Players = game.Players.Select(p => p.NextRound()).ToList();

                return round;
            }
            return Optional<Round>.Unspecified;
        }

        private async Task ShortCircuitGame(GameObject game)
        {
            Round lastRound = game.Rounds.Last();

            if (lastRound.BothTeamsLostRewards)
            {
                await BothLose(game);
            }
            else
            {
                await AutomaticallyResolveRounds(game, lastRound);
            }
        }

        private async Task BothLose(GameObject game)
        {
            GameScore score = game.Score();
            Player playerWithMoreRounds = game.Players.FirstOrDefault(p => p.TeamId == score.TeamId);
            Player playerWithLessRounds = game.Players.FirstOrDefault(p => p.TeamId != score.TeamId);

            await Task.WhenAll(
                game.Players.Select((player) =>
                    player.User.SendMessageAsync($"All players are out of coins. {playerWithMoreRounds.User.Username} won more rounds than {playerWithLessRounds.User.Username}, but doesn't qualify for a reward since he hasn't completed his collection.")));
        }

        private async Task AutomaticallyResolveRounds(GameObject game, Round lastRound)
        {
            int roundsWon = game.Rounds.Count(r => r.WinnerTeamId == lastRound.Winner.TeamId);
            int roundsLeftToWin = game.RoundsToReward - roundsWon;
            int coinsLeftAfterBuyingAtTheMinimumPrice = lastRound.Winner.CoinsLeft() - roundsLeftToWin * GameConfiguration.MinimumBet;

            // Can I avoid this nested loop and goto?
            for (int roundNumber = 1; roundNumber <= roundsLeftToWin; roundNumber++)
            {
                List<Player> players = new List<Player>();
                foreach (Player p in game.Players)
                {
                    if (p.TeamId == lastRound.Winner.TeamId && !p.IsBetValid(GameConfiguration.MinimumBet, false))
                    {
                        goto NoneGetRewards;
                    }
                    Player updatedPlayer = p.Bet(p.TeamId == lastRound.Winner.TeamId ? GameConfiguration.MinimumBet : 0);
                    players.Add(updatedPlayer);
                }
                game.Players = players;
                (Player winner, Player loser) = game.PlayersByTeamId(lastRound.Winner.TeamId);
                Round round = new Round(winner, loser, true);
                game.Players = game.Players.Select(p => p.NextRound()).ToList();
                game.Rounds.Add(round);
            }

            Player winnerByDefault = game.Players.FirstOrDefault(p => p.TeamId == lastRound.Winner.TeamId);
            game.Players = game.Players.Select(p => p.Win(lastRound.Winner.TeamId)).ToList();
           await Task.WhenAll(
                game.Players.Select((player) =>
                    player.User.SendMessageAsync($"{lastRound.Loser.User.Username} can no longer pay minimum price of {GameConfiguration.MinimumBet} coins. {winnerByDefault.User.Username} completes his collection by buying the remaining items at the minimum price of {GameConfiguration.MinimumBet} coins.")));
            return;

        NoneGetRewards:
        {
            GameScore score = game.Score();

            Player playerWithMoreRounds = game.Players.FirstOrDefault(p => p.TeamId == score.TeamId);
            game.Players = game.Players.Select(p => p.Win(score.TeamId)).ToList();
            await Task.WhenAll(
                game.Players.Select((player) =>
                    player.User.SendMessageAsync($"{lastRound.Loser.User.Username} can no longer pay minimum price of {GameConfiguration.MinimumBet} coins. {lastRound.Winner.User.Username} is missing {Math.Abs(coinsLeftAfterBuyingAtTheMinimumPrice)} coins to finish the set. No one qualifies for a reward. {playerWithMoreRounds.User.Username} wins more rounds.")));
        }
        }

        #endregion

        #region RoundLogic

        private async Task<Round> ExecuteRound(GameObject game, int roundNumber, bool isWar = false)
        {
            Optional<Team> winningTeam = await EvaluateWinnerOrWar(game, roundNumber, isWar);
            if (winningTeam.IsSpecified)
            {
                (Player winner, Player loser) = game.PlayersByTeamId(winningTeam.Value.TeamId);
                Round round = new Round(winner, loser);
                game.Players = game.Players.Select(p => p.NextRound()).ToList();
                return round;
            }
            else
            {
                await Task.WhenAll(
                  game.Players.Select(p =>
                    p.User.SendMessageAsync(PlayerMessage.War())));
                return await ExecuteRound(game, roundNumber, true);
            }
        }

        private async Task<Optional<Team>> EvaluateWinnerOrWar(GameObject game, int round, bool isWar)
        {
            Optional<SocketMessage>[] messages = await Task.WhenAll(
              game.Players.Select(p => RetryUntilValidBet(game, p, isWar)));

            if (messages.All(m => !m.IsSpecified))
            {
                throw new GameAbortException($"Both players didn't offer an amount in round number {round}.");
            }

            game.Players =
              game.Players.Select((player, index) =>
              {
                  Optional<SocketMessage> message = messages.FirstOrDefault(m => m.IsSpecified && m.Value.Author.Id == player.User.Id);
                  return UpdatePlayerBet(player, message, isWar);
              }).ToList();

            IOrderedEnumerable<Team> teams = game.Players
                .GroupBy(p => p.TeamId)
                .Select(grouping => new Team(grouping))
                .OrderByDescending(team => team.CurrentBet);

            Team winningTeam = teams.First();

            if (teams.Where(t => t.TeamId != winningTeam.TeamId).Any(t => t.CurrentBet == winningTeam.CurrentBet))
            {
                return await EvaluateWar(game, teams, messages);
            }

            return winningTeam;
        }

        private async Task<Optional<SocketMessage>> RetryUntilValidBet(GameObject game, Player player, bool isWar, int retries = 3)
        {
            if (retries == 0)
            {
                return Optional<SocketMessage>.Unspecified;
            }

            Optional<SocketMessage> message = await OptionalUtilities.TryAsync(ReadPlayerMessage(player));
            if (message.IsSpecified)
            {
                Nullable<int> betAmount = ParseMessageToInt(message.Value);

                // Retry on message not being a number
                if (!betAmount.HasValue)
                {
                    await player.User.SendMessageAsync(PlayerMessage.BetInfo(game.GameCoins));
                    return await RetryUntilValidBet(game, player, isWar, retries - 1);
                }

                // Retry on invalid bet
                if (!player.IsBetValid(betAmount.Value, isWar))
                {
                    string info = betAmount.Value < (isWar ? 1 : GameConfiguration.MinimumBet)
                      ? PlayerMessage.BetZero()
                      : PlayerMessage.BetOutsideBudget(player.Coins - player.CurrentBet);
                    await player.User.SendMessageAsync(info);
                    return await RetryUntilValidBet(game, player, isWar, retries - 1);
                }

                return message;
            }

            // Retry on timeout
            await player.User.SendMessageAsync(PlayerMessage.RoundTimeout(retries - 1));
            return await RetryUntilValidBet(game, player, isWar, retries - 1);
        }

        private async Task<Optional<Team>> EvaluateWar(GameObject game, IEnumerable<Team> teams, IEnumerable<Optional<SocketMessage>> messages)
        {
            // continue with war
            if (game.Players.All(p => p.CoinsLeft() > 0))
            {
                return Optional<Team>.Unspecified;
            }

            // race to victory
            if (game.Players.All(p => p.CoinsLeft() == 0))
            {
                Optional<SocketMessage> firstValidMessage = messages.FirstOrDefault(m => m.IsSpecified);
                Player winner = game.Players.FirstOrDefault(p => p.User.Id == firstValidMessage.Value.Author.Id);
                await Task.WhenAll(
                    game.Players.Select(p =>
                        p.User.SendMessageAsync($"Both players have 0 coins after the initial tied offer. {winner.User.Username} who made the offer first wins this round.")));
                await Task.Delay(5000);

                return teams.FirstOrDefault(t => t.TeamId == winner.TeamId);
            }

            // automatically resolve round
            Player playerWithCoinsLeft = game.Players.FirstOrDefault(p => p.CoinsLeft() != 0);
            Player playerWithNoCoinsLeft = game.Players.FirstOrDefault(p => p.CoinsLeft() == 0);
            playerWithCoinsLeft.Bet(1);
            await Task.WhenAll(
                    game.Players.Select(p =>
                        p.User.SendMessageAsync($"{playerWithNoCoinsLeft.User.Username} has can't increase his offer after the tie because he has no coins left. {playerWithCoinsLeft.User.Username} wins this round by increasing his offer by 1 automatically.")));
            await Task.Delay(5000);
            return teams.FirstOrDefault(t => t.TeamId == playerWithCoinsLeft.TeamId);
        }

        private Player UpdatePlayerBet(Player player, Optional<SocketMessage> message, bool isWar)
        {
            if(!message.IsSpecified)
            {
                int automaticBet = isWar ? 1 : GameConfiguration.MinimumBet;
                if (!player.IsBetValid(automaticBet, isWar))
                {
                    return player.Bet(0);
                }
                return player.Bet(automaticBet);
            }

            // here we know for sure bet is valid
            return player.Bet(ParseMessageToInt(message.Value).Value);
        }

        private Nullable<int> ParseMessageToInt(SocketMessage message)
        {
            if (!int.TryParse(message.Content, out int bet))
                return null;
            return bet;
        }

        #endregion

        #region Communication

        private async Task SendRoundMessage(GameObject game, int currentRound)
        {
            if (currentRound > 1)
            {
                await Task.WhenAll(
                    game.Players.Select((player) =>
                        player.User.SendMessageAsync(GameScoreView.Of(game, game.Players.First(), game.Players.Last()))));
                await Task.Delay(5000);
            }

            await Task.WhenAll(
                game.Players.Select((player) =>
                    player.User.SendMessageAsync(PlayerMessage.RoundStart(currentRound, player.Coins, game.Collectable.ItemName))));

        }

        private async Task SendEndOfGameMessage(GameObject game)
        {
            await Task.WhenAll(
                        game.Players.Select((player) =>
                            player.User.SendMessageAsync(GameScoreView.Of(game, game.Players.First(), game.Players.Last(), true))));

            await Task.Delay(5000);

            await Task.WhenAll(
                        game.Players.Select((player) =>
                            player.User.SendMessageAsync(EndOfGameMsg(player, game.Collectable.EmoteName))));
        }

        private string EndOfGameMsg(Player player, string itemEmote)
        {
            return player.Winner
              ? PlayerMessage.GameWin(player.Coins, itemEmote)
              : PlayerMessage.GameLose(itemEmote);
        }

        private Task<SocketMessage> ReadPlayerMessage(Player player)
        {
            return _module.NextMessageAsync(new EnsureFromUserCriterion(player.User.Id), RoundTimeoutTimeSpan);
        }

        #endregion
    }
}
