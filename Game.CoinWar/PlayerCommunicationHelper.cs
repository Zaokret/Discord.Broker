using DiscordBot.Game.CoinWar.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar
{
    public class PlayerMessage
    {
        public static string GameStart(string name, int startIn) => 
            $"{name} joined an auction. Start in {startIn} seconds.";

        public static string RoundStart(int round, int coins, string itemName) =>
            $"{itemName.Capitalize()} number {round} is out for display. You have {coins} coins left. Place your bet.";

        public static string War() => 
            $"Tied with another collector. Add to your initial offer!";

        public static string BetInfo(int coins) => 
            $"Please pick a number in range: [1, 2, 3 ... {coins - 2}, {coins - 1}, {coins}]";

        public static string RoundTimeout(int attempts) => 
            $"You haven't provided an answer in a acceptable time. Game will timeout {attempts} more times before you lose the round.";

        public static string BetZero() => 
            "You have to do better than that!";

        public static string BetOutsideBudget(int coins) => 
            $"You can't aford it. Funds available: {coins}";

        public static string GameWin(int coins, string itemEmote) => 
            $"Game over. You completed your {itemEmote} collection and leave with ${coins} coins in your pocket!";

        public static string GameLose(string itemEmote) => 
            $"Game over. You missed a chance to complete your {itemEmote} collection.";
    }
}
