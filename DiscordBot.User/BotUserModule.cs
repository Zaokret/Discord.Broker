using Discord;
using Discord.Commands;
using DiscordBot.Core.Attributes;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Awards
{
    public class BotUserModule : ModuleBase<SocketCommandContext>
    {
        private readonly GlobalConfiguration _config;

        public BotUserModule(GlobalConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        [RequiredModeratorRole]
        [Command("write")]
        [Summary("Write a message as a bot.")]
        public async Task Write(ulong channelID, string message)
        {
            var channel = await Context.Client.Rest.GetChannelAsync(channelID);
            if (channel != null && channel is IMessageChannel msgChannel)
            {
                var typing = msgChannel.EnterTypingState();
                await Task.Delay(2000).ContinueWith(async (t) => {
                    typing.Dispose();
                    await msgChannel.SendMessageAsync(message);
                });
            }
        }

        [RequiredModeratorRole]
        [Command("write")]
        [Summary("Write a message as a bot.")]
        public async Task Write(IChannel channelRef, string message)
        {
            var channel = await Context.Client.Rest.GetChannelAsync(channelRef.Id);
            if (channel != null && channel is IMessageChannel msgChannel)
            {
                var typing = msgChannel.EnterTypingState();
                await Task.Delay(2000).ContinueWith(async (t) => {
                    typing.Dispose();
                    await msgChannel.SendMessageAsync(message);
                });
            }
        }
    }
}
