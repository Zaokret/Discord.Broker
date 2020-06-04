using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Core.Attributes
{
    public class RequiredBotAuthorAttribute : PreconditionAttribute
    {
        public RequiredBotAuthorAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            GlobalConfiguration config = (GlobalConfiguration)services.GetService(typeof(GlobalConfiguration));
            if (context.User.Id == config.BotAuthor)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError($"Only bot author can run this command."));
            }
        }
    }
}
