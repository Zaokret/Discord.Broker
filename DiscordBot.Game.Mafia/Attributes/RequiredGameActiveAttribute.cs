using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia.Attributes
{
    class RequiredGameActiveAttribute : PreconditionAttribute
    {
        public RequiredGameActiveAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var service = (MafiaService)services.GetService(typeof(MafiaService));

            if (service.IsGameActive())
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command works only if game is active."));
            }
        }
    }
}
