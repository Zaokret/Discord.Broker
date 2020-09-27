using Discord.Commands;
using DiscordBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Escrow
{
    class RequiredJudgeAssignedAttribute : PreconditionAttribute
    {
        public RequiredJudgeAssignedAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var repo = (DynamicConfigurationRepository)services.GetService(typeof(DynamicConfigurationRepository));

            if (repo != null)
            {
                var config = repo.Get();
                if(config != null && config.JudgeId > 0) 
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            return Task.FromResult(PreconditionResult.FromError("Judge must be appointed before any bets can be created."));
        }
    }
}
