using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
  public class CommandHandler
  {
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly Configuration _config;

    public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands, Configuration config)
    {
      _services = services ?? throw new ArgumentNullException(nameof(services));
      _commands = commands ?? throw new ArgumentNullException(nameof(commands));
      _client = client ?? throw new ArgumentNullException(nameof(client));
      _config = config ?? throw new ArgumentNullException(nameof(config));

      _client.MessageReceived += HandleCommandAsync;
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
      if (!(messageParam is SocketUserMessage message)) return;

      int argPos = 0;

      if (!(message.HasCharPrefix(_config.CommandPrefix, ref argPos) ||
          message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
          message.Author.IsBot)
        return;

      var context = new SocketCommandContext(_client, message);

      await _commands.ExecuteAsync(
          context: context,
          argPos: argPos,
          services: _services);
    }
  }
}
