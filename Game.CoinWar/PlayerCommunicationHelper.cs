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

        public static string GameStart(string name, int startIn) => $"{name} joined an auction. Start in {startIn} seconds.";
        public static string RoundStart(int round, int coins) => $"Tiger number {round} is out for display. You have {coins} coins left.";
        public static string War() => $"Tied with another collector. Add to your initial offer!";
        public static string BetInfo(int coins) => $"Please pick a number in range: [1, 2, 3 ... {coins - 2}, {coins - 1}, {coins}]";
        public static string RoundTimeout(int attempts) => $"You haven't provided an answer in a acceptable time. Game will timeout {attempts} more times before you lose the round.";

        public static string BetZero() => "You have to do better than that!";
        public static string BetOutsideBudget(int coins) => $"You can't aford it. Funds available: {coins}";
        public static string GameWin() => $"You completed your collection!";
        public static string GameLose() => $"You missed a chance to complete your collection.";
    }
}
