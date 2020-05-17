using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
  class LoggingService
  {
    private readonly DiscordSocketClient _discord;
    private readonly CommandService _commands;

    public LoggingService(DiscordSocketClient discord, CommandService commands)
    {
      _discord = discord;
      _commands = commands;

      _discord.Log += OnLogAsync;
      _commands.Log += OnLogAsync;
    }

    private Task OnLogAsync(LogMessage msg)
    {
      string logText = $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
      return Console.Out.WriteLineAsync(logText);       // Write the log text to the console
    }
  }
}
