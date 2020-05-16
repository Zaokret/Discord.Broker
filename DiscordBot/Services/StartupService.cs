using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
  class StartupService
  {
    private readonly IServiceProvider _provider;
    private readonly DiscordSocketClient _discord;
    private readonly CommandService _commands;
    private readonly Configuration _config;

    public StartupService(
        IServiceProvider provider,
        DiscordSocketClient discord,
        CommandService commands,
        Configuration config)
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
        throw new Exception("Please enter bot's token into the `config.json` file found in the applications root directory.");

      await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
      await _discord.StartAsync();                                // Connect to the websocket

      await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);     // Load commands and modules into the command service
    }
  }
}
