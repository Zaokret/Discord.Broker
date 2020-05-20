using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar
{
    public class PlayerCommunicationHelper
    {
        
    }

    public class PlayerMessage
    {
        public static string GameCreated(string joinCommand) => $"Coin war game created. Player two must use this command to join the game: '{joinCommand}'";
        public static string GameNotFound() => $"Game you requested no longer exists.";

        public static string GameStart(string name, int startIn) => $"Player {name} accepted your challange. Game will start in {startIn} seconds.";
        public static string RoundStart(int round, int coins) => $"Round {round} has started. Reply with your bet out of {coins} coins.";
        public static string War() => $"You are at WAR! Round continues - place your bets.";
        public static string RoundWin(string name) => $"Winner of this round is {name}.";
        public static string BetInfo(int coins) => $"Please pick a number in range: [1, 2, 3 ... {coins - 2}, {coins - 1}, {coins}]";
        public static string RoundTimeout(int attempts) => $"You haven't provided an answer in a acceptable time. Game will timeout {attempts} more times before you lose the round.";

        public static string BetZero() => "Bet must be greater than zero.";
        public static string BetOutsideBudget(int coins) => $"Bet outside of your budget. Funds available: {coins}";
        public static string GameWin() => $"You WON!";
        public static string GameLose() => $"You LOST!";
    }
}
