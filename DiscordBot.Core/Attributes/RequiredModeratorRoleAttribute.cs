using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Core.Attributes
{
    public class RequiredModeratorRoleAttribute : PreconditionAttribute
    {
        public RequiredModeratorRoleAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            GlobalConfiguration config = (GlobalConfiguration)services.GetService(typeof(GlobalConfiguration));
            var user = context.Guild.GetUserAsync(context.User.Id).GetAwaiter().GetResult();
            if (user.RoleIds.Any(r => r == config.ModeratorRoleID))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError($"Only moderators can use this command."));
            }
        }
    }
}
