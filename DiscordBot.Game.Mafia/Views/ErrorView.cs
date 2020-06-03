using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Views
{
    public static class ErrorView
    {
        public static string MultipleGames() => 
            "There could be only one initiated game at a time. Wait until the active group finishes to initiate a game. " +
            "Message 'JJ 3maj' if you find yourself waiting for your time to play too often.";

        public static string NotFound() => "Game not found.";

        public static string GameExpired(int expiration) =>
            $"More than {expiration} minutes have passed since game was initiated. Initiate again.";

        public static string InProgress() => "You can't join game in progress.";

        public static string PlayerNotPlaying(string username) => $"{username} is not part of this game session.";

        public static string PlayerAlreadyInactive(string username) => 
            $"{username} is already excommunicated or sacrified.";

        public static string InvalidSacrificeTarget(string username) => 
            $"{username} is a cultist. You can't sacrifice one of your own.";

        public static string RepeatedSacrifice(string username) => 
            $"{username} was already successfully sacrificed.";

        public static string NotEnoughFunds() => 
            $"You need atleast {PriceConfiguration.CostOfEntry} coins to create/join this game.";

        public static string AlreadyInLobby() =>
            "You are already in the game lobby.";
    }

    
}
