using DiscordBot.Game.Mafia.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.Mafia.Views
{
    public static class GameElement
    {
        public static string Name = "Divide";

        public static class Channel
        {
            public static string Public() => "senate-of-the-attarian-republic";
            public static string Private() => "shrine-of-aves";
            public static string Command() => "command-hub";
        }

        public static string Phase(PhaseType phase)
        {
            string name = string.Empty;
            switch(phase)
            {
                case PhaseType.Day: name = "Day"; break;
                case PhaseType.Night: name = "Night"; break;
                case PhaseType.Investigation: name = "Visions"; break;
                default:
                    throw new NotImplementedException($"{nameof(phase)} : {phase}");
            }
            return name;
        }

        public static string Group(GroupType group)
        {
            string name = string.Empty;
            switch (group)
            {
                case GroupType.Informed: name = "Cultists"; break;
                case GroupType.Uninformed: name = "Moderates"; break;
                default:
                    throw new NotImplementedException($"{nameof(group)} : {group}");
            }
            return name;
        }

        public static IEnumerable<string> GetRoleNames()
        {
            foreach (GameRole role in Enum.GetValues(typeof(GameRole)))
            {
                yield return GameElement.Role(role);
            }
        }

        public static string Role(GameRole gameRole)
        {
            string name = string.Empty;
            switch (gameRole)
            {
                case GameRole.Killer: name = "Cultist"; break;
                case GameRole.Kicker: name = "Moderate"; break;
                case GameRole.Investigator: name = "Augur"; break;
                default:
                    throw new NotImplementedException($"{nameof(gameRole)} : {gameRole}");
            }
            return name;
        }
    }
}
