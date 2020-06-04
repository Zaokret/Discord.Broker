using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Views
{
    public static class InfoView
    {
        public static string LobbyCreated(string gameId) =>
            $"{GameElement.GameName} game was created. {GameConfiguration.NumberOfPlayers - 1} more players need to join to start the game. " +
            $"To join they must use command '$join'.";

        public static string LeftLobby() => "You left the game lobby.";

        public static string LobbyStatus(int usersInLobby) => $"{usersInLobby}/{GameConfiguration.NumberOfPlayers} users in lobby.";

        public static string GameStarting() => "Game lobby is full and game creation is in progress...";
    }
}
