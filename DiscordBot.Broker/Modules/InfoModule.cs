using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly GlobalConfiguration _config;
        private readonly TasteService _tasteService;

        public InfoModule(DiscordSocketClient client, GlobalConfiguration config, TasteService tasteService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tasteService = tasteService ?? throw new ArgumentNullException(nameof(client));
        }

        [Command("donate")]
        [Summary("Send donation link.")]
        public async Task Donate()
        {
            await ReplyAsync(string.Empty, false, EmbedViews.Donate(_config.PayPalUrl));
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

        [Command("media")]
        [Summary("Get info on media.")]
        public async Task GetMediaInfo(string media)
        {
            if(!string.IsNullOrWhiteSpace(media))
            {
                IEnumerable<TasteItem> info = await _tasteService.GetInfo(media);

                if (!info.Any())
                    await ReplyAsync($"No information found related with {media}.");

                await Task.WhenAll(info.Select(i => ReplyAsync("", false, EmbedViews.TasteItem(i))));
            }
            
        }

        [Command("recommend")]
        [Summary("Get recommendation based on media.")]
        public async Task GetMediaRecommendation(string media)
        {
            if (!string.IsNullOrWhiteSpace(media))
            {
                IEnumerable<TasteItem> recommendation = await _tasteService.GetRecommendation(media);
                if (!recommendation.Any())
                    await ReplyAsync($"No recommendation found based on {media}.");

                await Task.WhenAll(recommendation.Select(i => ReplyAsync("", false, EmbedViews.TasteItem(i))));
            }
        }

        [Command("commands")]
        public async Task Commands()
        {
            await ReplyAsync(string.Empty, false , EmbedViews.Commands(_client.CurrentUser, _config.PayPalUrl));
        }
    }
}
