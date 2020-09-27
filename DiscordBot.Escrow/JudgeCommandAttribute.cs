using Discord;
using Discord.Commands;
using DiscordBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Escrow
{
    class JudgeCommandAttribute : PreconditionAttribute
    {
        public JudgeCommandAttribute() { }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var repo = (DynamicConfigurationRepository)services.GetService(typeof(DynamicConfigurationRepository));

            var config = repo.Get();
            if (config.JudgeId == context.User.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError($"Only judge {MentionUtils.MentionUser(config.JudgeId)} can resolve bets."));
        }
    }
}
