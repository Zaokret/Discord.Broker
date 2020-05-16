using Discord;
using Discord.WebSocket;
using DiscordBot.Actions;
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
          => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
          _client = new DiscordSocketClient();
          _client.Log += Logger<DiscordSocketClient>.Log;
      
          Configuration _config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("config.json"));
      
          await _client.LoginAsync(TokenType.Bot, _config.Token);
          await _client.StartAsync();
      
          IUserRepository repo = new JsonUserRepository(new UserEntityContextProvider());

          ReactionController reactionController = new ReactionController(repo, _config);

          _client.ReactionAdded += reactionController.ReactionAdded;
          _client.ReactionRemoved += reactionController.ReactionRemoved;

          // Block this task until the program is closed.
          await Task.Delay(-1);
    }
  }
}
