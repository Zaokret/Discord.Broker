using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Core.Attributes
{
    public class RequiredCurrentUserAttribute : PreconditionAttribute
    {
        public RequiredCurrentUserAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var client = (DiscordSocketClient)services.GetService(typeof(DiscordSocketClient));

            if (context.User.Id == client.CurrentUser.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError($"Only {client.CurrentUser.Username} can run this command."));
            }
        }
    }
}
