using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Game.CoinWar.Exceptions;
using DiscordBot.Game.CoinWar.Extensions;
using DiscordBot.Game.CoinWar.Models;
using DiscordBot.Game.CoinWar.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Game.CoinWar
{
    public class CoinWarModule : InteractiveBase<SocketCommandContext>
    {
        private readonly GlobalConfiguration _config;
        private readonly GameService _service;
        private readonly DiscordSocketClient _client;

        public CoinWarModule(GlobalConfiguration config, GameService service, DiscordSocketClient client)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service.GiveModuleControl(this);
        }

        [Command("auction")]
        [Summary("Creates/Joins a coin war game instance.")]
        public async Task JoinOrCreate(IUser user)
        {
            if(Context.User.Id == user.Id)
            {
                await Context.User.SendMessageAsync("You can't play with yourself.");
            }
            else if (user.Id == _client.CurrentUser.Id)
            {
                await Context.User.SendMessageAsync("I'm still learning to play this game.");
            }
            else
            {
                try
                {
                    await _service.CreateOrStartGameAsync(Context.User, user, Context.Channel.Name);
                }
                catch (GameAbortException ex)
                {
                    await Task.WhenAll(new[]
                    {
                        user.SendMessageAsync($"Game was aborted, reason: {ex}"),
                        Context.User.SendMessageAsync($"Game was aborted, reason: {ex}")
                    });
                }
                catch(Exception ex)
                {
                    await Task.WhenAll(new[]
                    {
                        user.SendMessageAsync($"Game was aborted for unknown reason. Forward this message to 'JJ 3maj'. {ex}."),
                        Context.User.SendMessageAsync($"Game was aborted for unknown reason. Forward this message to 'JJ 3maj'. {ex}")
                    });
                }
            }
        }
    }
}
