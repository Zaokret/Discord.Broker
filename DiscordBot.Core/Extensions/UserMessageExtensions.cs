using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Core.Extensions
{
    public static class UserMessageExtensions
    {
        public static async Task<IUserMessage> SendMessageAsync(this IUser user, Embed embed)
        {
            return await user.SendMessageAsync(string.Empty, false, embed);
        }
    }
}
