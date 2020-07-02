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
        public static void Main(string[] args)
          => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            await Console.Out.WriteLineAsync("Enter a valid discord bot token:");
            string token = await Console.In.ReadLineAsync();

            await Console.Out.WriteLineAsync("Enter your paypal business:");
            string business = await Console.In.ReadLineAsync();

            await Startup.RunAsync(new[] { token, business });
            await Task.Delay(-1); // Keep the program alive
        }
    }
}
