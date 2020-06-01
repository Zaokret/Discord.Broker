using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.Mafia.Views
{
    public static class GameRules
    {
        public static IEnumerable<Embed> Of()
        {
            IEnumerable<string> generalDescription = new[] {
                "At the start of the game, each player is secretly assigned a role affiliated with **moderates** or **cultists**.",
                "Every cultist is given the identities of their cult members, whereas the moderates only receive the number of cultists in the game, and do not know which players are cultists and which are moderates.",
                "The game has two alternating phases: first, a **night phase**, during which cultists may **covertly abduct and sacrifice** other players, and second, a **day phase**, in which surviving players debate the identities of players and **vote to excommunicate** a suspect.",
                "The game continues until a faction achieves its win condition; for the moderates, this means **eliminating the cult**, while for the cultists this means **reaching numerical parity** with the moderates.",
                "One player is given a special role of **augur** who may learn the team of one player every night.",
            };
            
            IEnumerable<string> gameFlowDescription = new[]
            {
                "Everybody confirm they are ready for game to start",
                "Game starts with the night phase.",
                $"Cultists have ten minutes to pick their target by executing '$excommunicate @moderate' command and placing their vote unanimously. This command can be only be executed at night in {GameElement.Channel.Private()} channel.",
                $"Once cultists have made their pick, augur has four minutes to pick a player by executing '$signs @player' command as a private message to the AttarcoinBot to keep his pick private.",
                $"After augur learns with whom, player he has picked, is affiliated, cult's victim is revelead to the {GameElement.Channel.Public()} channel and day phase begins.",
                $"Everyone introduces themselfs and discussion ensues among the living players.",
                $"At any point during this phase, a player may accuse someone of being a cultist and prompt others to vote to eliminate them by executing '$excommunicate @player' in the {GameElement.Channel.Public()} channel.",
                "If over half of the players do so, the accused person is eliminated and night begins.",
                "Otherwise, the phase continues until an elimination occurs or ten minutes passes."
            };

            IEnumerable<string> rewardDescriptions = new[]
            {
                $"{PriceConfiguration.CoinsPerLivedRound} coins per round you're alive for all roles",
                $"{PriceConfiguration.CoinsPerExcommunicationOfCultists} coins per cultist excommunication for moderates",
                $"{PriceConfiguration.CoinsPerSacrifice} coins per sacrifice for cultists",
                $"{PriceConfiguration.AugurSacrificeMultiplayer} augur sacrifice coin multiplier for cultists",
                "You're eligable for coins if you were alive at the point an event happend.",
                $"* to play you must have at least {PriceConfiguration.CostOfEntry} coins to pay the broker."
            };
            
            Embed general = new EmbedBuilder()
                .WithTitle($"🏛    M O D E R A T E S    V S    C U L T I S T S    🏚️")
                .WithDescription(string.Join("\n", generalDescription))
                .WithColor(Color.DarkRed)
                .Build();

            Embed gameFlow = new EmbedBuilder()
                .WithTitle($"🏛    G A M E    F L O W    🏚️")
                .WithDescription(string.Join("\n", gameFlowDescription))
                .WithFooter($"Group voice chat is encouraged. Otherwise, refrain from talking to players outside of {GameElement.Channel.Public()} and {GameElement.Channel.Private()} channels during the game.")
                .WithColor(Color.DarkRed)
                .Build();

            Embed rewards = new EmbedBuilder()
                .WithTitle($"🏛    C O I N    B R E A K D O W N    🏚️")
                .WithDescription(string.Join("\n", rewardDescriptions))
                .WithColor(Color.DarkRed)
                .Build();

            return new[]
            {
                general, gameFlow, rewards
            };
        }
    }
}
