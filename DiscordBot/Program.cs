using Discord;
using Discord.WebSocket;
using DiscordBot.Controllers;
using DiscordBot.Contexts;
using DiscordBot.Contracts;
using DiscordBot.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;

        public static void Main(string[] args)
          => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
          await Startup.RunAsync(args);
          await Task.Delay(-1); // Keep the program alive
        }
    }
}
