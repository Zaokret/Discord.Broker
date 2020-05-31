using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;
using Serilog.Formatting;
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

        private readonly string LogTemplate = "{0:hh:mm:ss} [{1}] {2}: {3}";
        private readonly Logger Log = new LoggerConfiguration()
                .WriteTo.File("log.txt")
                .CreateLogger();

        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            string logMessage = string.Format(LogTemplate, DateTime.UtcNow, msg.Severity, msg.Source, msg.Exception?.ToString() ?? msg.Message);
            if (msg.Severity < LogSeverity.Info)
            {
                Log.Error(logMessage);
            }
            return Console.Out.WriteLineAsync(logMessage);
        }
    }
}
