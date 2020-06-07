using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    public class PollModule : ModuleBase<SocketCommandContext>
    {
        private readonly PollService _service;
        public PollModule(PollService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [Command("poll")]
        [Summary("Creates a poll.")]
        public async Task GetCoinsAsync(List<string> options, PollCommandArguments args)
        {
            Optional<string> errorMessage = _service.ValidatePollCommandArguments(args, options);
            if(errorMessage.IsSpecified)
            {
                await ReplyAsync(errorMessage.Value);
            }
            else
            {
                Poll poll = _service.CreatePoll(args, options, Context.User);
                IUserMessage pollMessage = await ReplyAsync(string.Empty, false, poll.Message);
                await pollMessage.AddReactionsAsync(poll.Emojis.Select(e => new Emoji(e)).ToArray());
            }
        }
    }
}
