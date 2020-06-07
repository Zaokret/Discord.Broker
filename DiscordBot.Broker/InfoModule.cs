using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        public InfoModule(DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        [Command("help")]
        [Summary("Send help instructions.")]
        public async Task Help()
        {
            string filePath = "./assets/guide-to-staying-relaxed.mp3";
            if (Context.Channel != null)
                await Context.Channel.SendFileAsync(filePath);
            else
                await Context.User.SendFileAsync(filePath);
        }

        [Command("poll")]
        [Summary("Creates a poll.")]
        public async Task GetCoinsAsync()
        {
            await ReplyAsync("Create a poll by sending a message [$poll \"optionName1, optionName2, optionName3\" Title: \"Title Text\" Description: \"Description Text\"] Send \"\" instead of options to create a yes/no poll.");
        }

        [Command("commands")]
        public async Task Commands()
        {
            string[] coins = new string[]
            {
                "$coins - check your balance",
                "$coins @user - check user's balance",
                "$transfer X @user - transfer X number of coins to user",
                "$rich - check 5 richest users and your rank"
            };

            string[] polls = new string[]
            {
                "$poll \"optionName1, optionName2, optionName3\" Title: \"Title Text\" Description: \"Description Text\" - vote between options",
                "$poll \"\" Title: \"Title Text\" Description: \"Description Text\" - vote yes/no"
            };

            string auction = "$auction @user - challange user or accept user's challange to an auction game";

            string[] occult = new[] 
            {
                "$occult - create game lobby",
                "$join - join game lobby",
                "$leave - leave game lobby",
                "$role @user - vote user's role in the game",
                "$excommunicate @user - everyone's in game command: vote to remove suspected cultist from the game",
                "$sacrifice @user - cultist's in game command: vote to remove one of moderates from the game"
            };

            string help = "$help - provides you with first-grade relaxation audiotape";

            Embed message = new EmbedBuilder()
                .WithAuthor(_client.CurrentUser)
                .WithTitle("Commands")
                .AddField("Coins", string.Join("\n", coins))
                .AddField("Auction", auction)
                .AddField("Occult", string.Join("\n", occult))
                .AddField("Poll", string.Join("\n", polls))
                .AddField("Help", help)
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(string.Empty, false , message);
        }
    }
}
