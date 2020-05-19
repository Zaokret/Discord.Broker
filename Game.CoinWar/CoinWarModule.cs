using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
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

        public CoinWarModule(GlobalConfiguration config, GameService service)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _service.GiveModuleControl(this);
        }

        private const string GameInitCommand = "coinwar";
        private string JoinCommand(string id) => $"{_config.CommandPrefix}{GameInitCommand} {id}";

        [Command(GameInitCommand)]
        [Summary("Creates a coin war game instance.")]
        public async Task CreateCoinWarGame()
        {
            IUser user = Context.User;
            string gameId = _service.CreatePendingGame(user.Id);
            await user.SendMessageAsync(PlayerMessage.GameCreated(JoinCommand(gameId)));
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
