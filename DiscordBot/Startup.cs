using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Controllers;
using DiscordBot.Contexts;
using DiscordBot.Contracts;
using DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
  public class Startup
  {
    public Configuration Configuration { get; }

    public Startup(string[] args)
    {
      Configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("config.json"));
    }

    public static async Task RunAsync(string[] args)
    {
      var startup = new Startup(args);
      await startup.RunAsync();
    }

    public async Task RunAsync()
    {
      IServiceCollection services = new ServiceCollection();             
      ConfigureServices(services);
      var provider = services.BuildServiceProvider();
      
      provider.GetRequiredService<LoggingService>();      
      provider.GetRequiredService<CommandHandler>();
      provider.GetRequiredService<ReactionController>();

      await provider.GetRequiredService<StartupService>().StartAsync();
    }

    private void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
      {                                       // Add discord to the collection
        LogLevel = LogSeverity.Verbose,       // Tell the logger to give Verbose amount of info
        MessageCacheSize = 1000               // Cache 1,000 messages per channel
      }))
      .AddSingleton(new CommandService(new CommandServiceConfig
      {                                       // Add the command service to the collection
        LogLevel = LogSeverity.Verbose,       // Tell the logger to give Verbose amount of info
        DefaultRunMode = RunMode.Async,       // Force all commands to run async by default
      }))
      .AddSingleton<CommandHandler>()  // Add the command handler to the collection
      .AddSingleton<StartupService>()         // Add startupservice to the collection
      .AddSingleton<LoggingService>()         // Add loggingservice to the collection
      .AddSingleton<IUserRepository, JsonUserRepository>()
      .AddSingleton<UserEntityContextProvider>()
      .AddSingleton<ReactionController>()
      .AddSingleton(Configuration);           // Add the configuration to the collection
    }
  }
}
