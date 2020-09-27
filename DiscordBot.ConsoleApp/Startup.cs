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
using DiscordBot.Broker;
using Discord.Addons.Interactive;
using DiscordBot.Game.CoinWar;
using DiscordBot.Infrastructure.Repositories;
using DiscordBot.ConsoleApp;
using DiscordBot.Game.Mafia;
using System.Net.Http;
using DiscordBot.Escrow;
using DiscordBot.Infrastructure.Contexts;

namespace DiscordBot
{
    public class Startup
    {
        public GlobalConfiguration Configuration { get; }

        public Startup(string discordToken, string tasteToken)
        {
            JsonConfiguration jsonConfig = JsonConvert.DeserializeObject<JsonConfiguration>(File.ReadAllText("config.json"));
            Configuration = new GlobalConfiguration(discordToken, tasteToken, jsonConfig);
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args[0], args[1]);
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            IServiceCollection services = new ServiceCollection();             
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();
      
            provider.GetRequiredService<LoggingService>();      
            provider.GetRequiredService<CommandHandler>();

            if(EnvironmentConfiguration.IsProduction())
            {
                provider.GetRequiredService<ReactionController>();
            }

            await provider.GetRequiredService<StartupService>().StartAsync();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {                                       
                LogLevel = LogSeverity.Verbose,       
                MessageCacheSize = 1000              
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {                                       
                LogLevel = LogSeverity.Verbose,       
                DefaultRunMode = RunMode.Async,       
            }))
            .AddSingleton<HttpClient>()
            .AddSingleton<CommandHandler>()  
            .AddSingleton<StartupService>()         
            .AddSingleton<LoggingService>()         
            .AddSingleton<IUserRepository, JsonUserRepository>()
            .AddSingleton<CollectableRepository>()
            .AddSingleton<DynamicConfigurationRepository>()
            .AddSingleton<BetRepository>()
            .AddSingleton<UserEntityContextProvider>()
            .AddSingleton<BetEntityContextProvider>()
            .AddSingleton<ReactionController>()
            .AddSingleton<GameService>() // should this stay singleton ? 
            .AddSingleton<CollectablePickerService>()
            .AddSingleton<MafiaService>()
            .AddScoped<CoinService>()
            .AddScoped<TasteService>()
            .AddScoped<BetService>()
            .AddTransient<InteractiveService>()
            .AddTransient<PollService>()
            .AddSingleton(Configuration);           
        }
  }
}
