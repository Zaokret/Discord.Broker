using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Game.CoinWar.Extensions;
using DiscordBot.Game.CoinWar.Models;
using DiscordBot.Game.CoinWar.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Game.CoinWar
{
    class GuidHelper
    {
        public static string Generate()
        {
            return Guid.NewGuid().ToString("N");
        }
    }

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

        private const string GameInitCommand = "tigerking";
        private string JoinCommand(string id) => $"{_config.CommandPrefix}{GameInitCommand} {id}";

        /*
         GuildEmote emote = _client.Guilds.SelectMany(g => g.Emotes).FirstOrDefault(e => e.Name == "Attar_Coin");
            if(emote != null)
                await Context.Message.AddReactionAsync(emote);
        */

        /*
            command user ( challange someone )
            command user ( accept challange )
                remove pending challanges on challange or accept
        */

        [Command(GameInitCommand)]
        [Summary("Creates a coin war game instance.")]
        public async Task CreateCoinWarGame()
        {
            IUser user = Context.User;
            string gameId = _service.CreatePendingGame(user.Id);
            await user.SendMessageAsync(PlayerMessage.GameCreated(JoinCommand(gameId)), false, GameView.Info(new GameObject(new PendingGame(1), user, user)));
        }

        [Command(GameInitCommand, RunMode = RunMode.Async)]
        [Summary("Join coin war game that was created by another player.")]
        public async Task JoinCoinWarGame(string gameId)
        {
            IUser user = Context.User;
            bool gameFoundAndStarted = await _service.FindPendingGameAndStartAsync(gameId, user);

            if(gameFoundAndStarted == false)
            {
                await user.SendMessageAsync(PlayerMessage.GameNotFound());
            }
        }

    }
}
