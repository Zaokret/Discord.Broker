using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia.Attributes
{
    class RequiredGamePlayerAttribute : PreconditionAttribute
    {
        public RequiredGamePlayerAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var service = (MafiaService)services.GetService(typeof(MafiaService));

            if (service.IsGameActive() && service.IsUserPlaying(context.User.Id)) 
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command works only if you are playing the game."));
            }
        }
    }
}
