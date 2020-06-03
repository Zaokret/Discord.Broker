using DiscordBot.Broker;
using DiscordBot.Core.Utilities;
using DiscordBot.Game.Mafia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.Mafia.Views
{
    public static class GameElement
    {
        public static string Name = "Divide";

        public static class Channel
        {
            public static string Public() => "senate-of-the-attarian-republic";
            public static string Private() => "cult-of-aves";
            public static string Command() => "command-hub";
        }

        public static class ChannelDescription 
        {
            public static string PlayerList(List<Player> players) => string.Join(", ", players.Select(p => Mention.Of(p.User.Id)));
            public static string Public() => "command [$excommunicate @someone] to begin a vote";
            public static string Private(List<Player> uninformed) => $"command [$sacrifice @moderate] to begin a vote. Moderates: {PlayerList(uninformed)}. ";
            public static string Command() => "command-hub";
            public static string Augur(List<Player> players) => $"command [$signs @someone] to check what team he belongs to. Players still alive: {PlayerList(players)}";
        }

        public static string MurderScene(string nameOfVictim)
            => $"Light reveals an unholy act of murder. {nameOfVictim} lies bloodless at the senate entrance.";

        public static string NoMurder()
            => "Senate entrance is unobstructed by human bodies.";

        public static string VictimChosen(string nameOfVictim)
            => $"You will try to please your Master with {nameOfVictim}'s sacrifice.";

        public static string VictimNotChosen()
            => "There will be no sacrifice for your Master tonight.";

        public static string NoSigns()
            => $"Birds flew away before you had the chance to see them.";

        public static string SeenTheSigns(Player targetPlayer)
            => $"{targetPlayer.User.Username} flocks with {GameElement.Group(targetPlayer.Group)}.";

        public static string PlayerRemoved(string name)
            => $"{name} has lost the trust of the majority and believed to practice the dark arts.";

        public static string NoPlayerRemoved()
            => "Indecisiveness will certainly cost you your lives. Chaos rules in the senate.";

        public static string Phase(PhaseType phase)
        {
            string name = string.Empty;
            switch (phase)
            {
                case PhaseType.Day: name = "Day"; break;
                case PhaseType.Night: name = "Night"; break;
                case PhaseType.Investigation: name = "Signs"; break;
                default:
                    throw new NotImplementedException($"{nameof(phase)} : {phase}");
            }
            return name;
        }

        public static string PhasePhrase(PhaseType phase)
        {
            string name = string.Empty;
            switch (phase)
            {
                case PhaseType.Day: name = $"Daylight beckons senators to the {Channel.Public()}."; break;
                case PhaseType.Night: name = $"Sun bleeds bright red on the horizon as cultist gather at the {Channel.Private()}."; break;
                case PhaseType.Investigation: name = "Taking the auspices..."; break;
                default:
                    throw new NotImplementedException($"{nameof(phase)} : {phase}");
            }
            return name;
        }

        public static string PhaseInstruction(PhaseType phase)
        {
            string name = string.Empty;
            switch (phase)
            {
                case PhaseType.Day: name = $"Now speak your truth."; break;
                case PhaseType.Night: name = $"Slaughter in the master's name."; break;
                case PhaseType.Investigation: name = "Read the signs in the bird flight patterns."; break;
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

        public static class Poll
        {
            public static PollCommandArguments Kill(string name)
            {
                return new PollCommandArguments
                {
                    Title = $"Sacrifice vote",
                    Description = $"Will {name}'s sacrifice please our Master ?"
                };
            }

            public static PollCommandArguments Kick(string name)
            {
                return new PollCommandArguments
                {
                    Title = $"Excommunicate vote",
                    Description = $"Is {name} a {GameElement.Role(GameRole.Killer)} ?"
                };
            }

            public static PollCommandArguments Ready()
            {
                return new PollCommandArguments
                {
                    Title = "Are you ready ?",
                    Description = "Game will start when everyone votes yes."
                };
            }

            public static PollCommandArguments Role(string name)
            {
                return new PollCommandArguments
                {
                    Title = $"What is {name}'s role ?",
                    Description = $"Vote for the role you believe {name} has."
                };
            }

            public static PollCommandArguments Investigate()
            {
                return new PollCommandArguments
                {
                    Title = $"Intuit someone's group affiliation",
                    Description = $"Place your vote for one of the following alive players."
                };
            }
        }
    }
}
