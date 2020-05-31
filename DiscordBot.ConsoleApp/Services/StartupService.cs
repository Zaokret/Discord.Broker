using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Modules;
using DiscordBot.Game.CoinWar;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Broker;
using DiscordBot.ConsoleApp;

namespace DiscordBot.Services
{
    class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly GlobalConfiguration _config;

        public StartupService(
            IServiceProvider provider,
            DiscordSocketClient discord,
            CommandService commands,
            GlobalConfiguration config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            string discordToken = _config.Token;
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter a valid bot's token.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);     
            await _discord.StartAsync();                                

            _commands.AddTypeReader<List<string>>(new ListOfStringTypeReader());

            if(EnvironmentConfiguration.IsProduction())
            {
                await _commands.AddModuleAsync(typeof(CoinModule), _provider);
                await _commands.AddModuleAsync(typeof(CoinWarModule), _provider);
                await _commands.AddModuleAsync(typeof(PollModule), _provider);
            }
        }
    }
}
