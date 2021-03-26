using Discord;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Awards
{
    public class AwardService
    {
        private readonly DiscordSocketClient _client;
        private readonly CoinService _coinService;

        public AwardService(DiscordSocketClient client, CoinService coinService)
        {
            _coinService = coinService ?? throw new ArgumentNullException(nameof(coinService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<(IUser, Optional<EmbedBuilder>)> GetAward(SocketUserMessage post, IUser giver)
        {
            var guild = _client.GetGuild(post.Reference.GuildId.Value);
            var channel = guild.GetTextChannel(post.Reference.ChannelId);
            var message = await channel.GetMessageAsync(post.Reference.MessageId.Value);
            var content = string.IsNullOrWhiteSpace(message.Content) ? "link" : message.Content;
            var urlToMessage = MessageMentionUtils.Url(guild.Id, channel.Id, message.Id);

            if (message.Author.Id != giver.Id)
            {
                var embed = new EmbedBuilder()
                .WithAuthor(message.Author)
                .WithDescription($"[{content}]({urlToMessage})\n\nAwarded by {MentionUtils.MentionUser(giver.Id)}.")
                .WithUrl(urlToMessage)
                .WithTimestamp(message.EditedTimestamp ?? message.Timestamp);
                return (message.Author, embed);
            }
            else
            {
                return (message.Author, Optional<EmbedBuilder>.Unspecified);
            }
        }

        public async Task Transfer(ulong giverID, ulong receiverID, int amount)
        {
            await _coinService.RemoveFunds(giverID, amount);
            await _coinService.AddFunds(receiverID, amount);
        }
    }
}
