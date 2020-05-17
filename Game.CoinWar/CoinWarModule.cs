
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Game.CoinWar
{
  class Team
  {
    public Team(IGrouping<int, Player> grouping)
    {
      Coins = grouping.Aggregate(0, (teamBetAmount, player) => teamBetAmount + player.CurrentBet);
      Players = grouping.Select(p => p);
      TeamId = grouping.Key;
    }
    public int Coins { get; set; }
    public int TeamId { get; set; }
    public IEnumerable<Player> Players { get; set; }
  }

  class Player
  {
    public IUser User { get; set; }
    public int CurrentBet { get; set; }
    public int Coins { get; set; }
    public int TeamId { get; set; }
    public bool Winner { get; set; }

    public Player Win()
    {
      Winner = true;
      return this;
    }

    public bool IsBetValid(int bet)
    {
      return bet > 0 && (bet + CurrentBet) <= Coins;
    }

    public Player Bet(int bet)
    {
      CurrentBet = bet;
      return this;
    }

    public Player NextRound()
    {
      Coins -= CurrentBet;
      CurrentBet = 0;
      return this;
    }
  }

  class Round
  {
    public Round(int teamId)
    {
      WinnerTeamId = teamId;
      BothTeamLost = false;
    }

    public static Round BothLost()
    {
      return new Round(0) { BothTeamLost = true };
    }

    public bool BothTeamLost { get; set; }
    public int WinnerTeamId { get; set; }
  }

  class PendingGame
  {
    public PendingGame(ulong userId)
    {
      Guid = GuidHelper.Generate();
      UserInitiatorId = userId;
    }
    public string Guid { get; set; }
    public ulong UserInitiatorId { get; set; }
  }

  class Game
  {
    public Game(PendingGame game, IUser playerOne, IUser playerTwo)
    {
      Guid = game.Guid;
      NumberOfRounds = 1;
      RoundsToVictory = (int)decimal.Ceiling(new decimal(NumberOfRounds) / 2);
      BuyInInvestment = 1f;
      GameCoins = 100;
      Rounds = new List<Round>();
      Players = new List<Player>(new[]
      {
        new Player() { User = playerOne, Coins = GameCoins, CurrentBet = 0, TeamId = 1, Winner = false },
        new Player() { User = playerTwo, Coins = GameCoins, CurrentBet = 0, TeamId = 2, Winner = false }
      });
      WinnerFound = false;
    }
    public int RoundsToVictory { get; set; }
    public string Guid { get; set; }
    public int NumberOfRounds { get; set; }
    public ICollection<Round> Rounds { get; set; }
    public float BuyInInvestment { get; set; }
    public int GameCoins { get; set; }
    public ICollection<Player> Players { get; set; }
    public bool WinnerFound { get; set; }

    public void EndWithWinner(int teamId)
    {
      // will this update game players?
      Players
          .Where(p => p.TeamId == teamId)
          .Select(p => p.Win())
          .ToList();
      WinnerFound = true;
    }
  }

  class GameScore
  {
    public GameScore(int teamId, int roundsWon)
    {
      RoundsWon = roundsWon;
      TeamId = teamId;
    }
    public int RoundsWon { get; set; }
    public int TeamId { get; set; }
  }

  class GuidHelper
  {
    public static string Generate()
    {
      return Guid.NewGuid().ToString("N");
    }
  }

  public class CoinWarModule : InteractiveBase<SocketCommandContext>
  {
    private readonly Configuration _config;
    private readonly DiscordSocketClient _client;
    public CoinWarModule(Configuration config, DiscordSocketClient client)
    {
      _client = client ?? throw new ArgumentNullException(nameof(client));
      _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    private static ICollection<PendingGame> PendingGames { get; set; }

    private const string GameInitCommand = "coinwar";
    private string JoinCommand (string id) => $"{_config.CommandPrefix}{GameInitCommand} {id}";

    [Command(GameInitCommand)]
    [Summary("Creates a coin war game instance.")]
    public async Task CreateCoinWarGame()
    {
      IUser user = Context.User;
      string gameId = InitializeGame(user.Id);
      await user.SendMessageAsync($"Coin war game created. Player two must use this command to join the game: '{JoinCommand(gameId)}'");
    }

    [Command(GameInitCommand, RunMode = RunMode.Async)]
    [Summary("Join coin war game that was created by another player.")]
    public async Task JoinCoinWarGame(string gameId)
    {
      
      IUser user = Context.User;
      
      if (PendingGames != null)
      {
        PendingGame game = PendingGames.FirstOrDefault(g => g.Guid == gameId);
        if(game != null)
        {
          PendingGames.Remove(game);
          await SpawnGame(game, user);
        }
      }
      else
      {
        await user.SendMessageAsync($"Game you requested no longer exists.");
      }
      
    }

    private async Task SpawnGame(PendingGame pendingGame, IUser acceptingUser)
    {
      int delayUntilStart = 5;
      IUser userInitiator = await _client.Rest.GetUserAsync(pendingGame.UserInitiatorId);

      await Task.WhenAll(new[]
      {
        userInitiator.SendMessageAsync($"Player {acceptingUser.Username} accepted your challange. Game will start in {delayUntilStart} seconds."),
        acceptingUser.SendMessageAsync($"Player {acceptingUser.Username} accepted your challange. Game will start in {delayUntilStart} seconds.")
      });

      await Task.Delay(delayUntilStart * 1000);
      await StartGame(pendingGame, userInitiator, acceptingUser);
    }

    private async Task StartGame(PendingGame pendingGame, IUser userOne, IUser userTwo)
    {
      Game game = new Game(pendingGame, userOne, userTwo);

      for (int currentRound = 1; currentRound <= game.NumberOfRounds; currentRound++)
      {
        string roundMsgTemplate = "Round {0} has started. Reply with your bet out of {1} coins.";
        await Task.WhenAll(
          game.Players.Select((player) =>
            player.User.SendMessageAsync(string.Format(roundMsgTemplate, currentRound, player.Coins))));

        Optional<Round> fastRound = ShortCircuitRound(game);
        if(fastRound.IsSpecified)
        {
          game.Rounds.Add(fastRound.Value);
          continue;
        }

        Round round = await ExecuteRound(game, currentRound);
        game.Rounds.Add(round);
        if(game.Rounds.Count(r => r.WinnerTeamId == round.WinnerTeamId) == game.RoundsToVictory)
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

    private Optional<Round> ShortCircuitRound(Game game)
    {
      IEnumerable<Team> teams = game.Players
          .GroupBy(p => p.TeamId)
          .Select(grouping => new Team(grouping));

      if (teams.All(t => t.Coins == 0))
      {
        return Round.BothLost();
      }
      Team losingTeam = teams.FirstOrDefault(t => t.Coins == 0);
      if (losingTeam != null)
        return new Round(losingTeam.TeamId);

      return Optional<Round>.Unspecified;
    }

    private async Task<Round> ExecuteRound(Game game, int round)
    {
      Optional<Team> winningTeam = await EvaluateWinner(game, round);
      if (!winningTeam.IsSpecified)
      {
        await Task.WhenAll(
          game.Players.Select(p =>
            p.User.SendMessageAsync($"You are at WAR! Round continues - place your bets.")));
        await ExecuteRound(game, round);
      }

      game.Players = game.Players.Select(p => p.NextRound()).ToList();
      await Task.WhenAll(
        game.Players.Select(p =>
          p.User.SendMessageAsync($"Winner of this round is player {winningTeam.Value.TeamId}.")));
      return new Round(winningTeam.Value.TeamId);
    }

    private async Task<Optional<Team>> EvaluateWinner(Game game, int round)
    {
      SocketMessage[] messages = await Task.WhenAll(
        game.Players.Select((player) =>
          NextMessageAsync(new EnsureFromUserCriterion(player.User.Id))));

      game.Players = await Task.WhenAll(
        game.Players.Select((player, index) =>
          UpdatePlayerBet(game, messages[index], 2)));

      IOrderedEnumerable<Team> teams = game.Players
          .GroupBy(p => p.TeamId)
          .Select(grouping => new Team(grouping))
          .OrderByDescending(team => team.Coins);
      
      Team winningTeam = teams.First();
      if(teams.Any(t => t.Coins == winningTeam.Coins))
        return Optional<Team>.Unspecified;

      game.Players = teams.SelectMany(team => team.Players).ToList();
      return winningTeam;
    }

    private async Task<Player> UpdatePlayerBet(Game game, SocketMessage message, int retries)
    {
      Player player = game.Players.First(p => p.User.Id == message.Author.Id);
      if (retries == 0)
      {
        return player.Bet(0);
      }
      Nullable<int> betAmount = ParseMessageToInt(message);
      if(!betAmount.HasValue)
      {
        string info = $"Please pick a number in range: [1, 2, 3 ... {game.GameCoins - 2}, {game.GameCoins - 1}, {game.GameCoins}]";
        await player.User.SendMessageAsync(info);
        SocketMessage nextBetAttempt = await NextMessageAsync(new EnsureFromUserCriterion(player.User.Id));
        await UpdatePlayerBet(game, nextBetAttempt, retries - 1);
      }
      
      if(!player.IsBetValid(betAmount.Value))
      {
        string info = betAmount.Value == 0
          ? "Bet must be greater than zero."
          : $"Bet outside of your budget. Funds available: {player.Coins}";
        await player.User.SendMessageAsync(info);
        SocketMessage nextBetAttempt = await NextMessageAsync(new EnsureFromUserCriterion(player.User.Id));
        await UpdatePlayerBet(game, nextBetAttempt, retries - 1);
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
      if(PendingGames != null)
      {
        foreach (PendingGame existingGame in PendingGames.Where(g => g.UserInitiatorId == userId))
        {
          PendingGames.Remove(existingGame);
        }
      }
    }

    private string InitializeGame(ulong userId)
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
        ? $"You WON!"
        : $"You LOST!";
    }

    /*
    var eb = new EmbedBuilder()
        .WithColor(Color.DarkBlue)
        .WithTitle("Coin War!")
        .WithDescription("Let the games begin.")
        .Build();
     */
  }
}
