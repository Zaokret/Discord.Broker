using Discord;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Contracts;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Controllers
{
  public class ReactionController
  {
    private readonly IUserRepository _repository;
    private readonly Configuration _config;
    private readonly CoinService _service;

    public ReactionController(DiscordSocketClient client, IUserRepository repository, Configuration config, CoinService service)
    {
      _service = service ?? throw new ArgumentNullException(nameof(service));
      _config = config ?? throw new ArgumentNullException(nameof(config));
      _repository = repository ?? throw new ArgumentNullException(nameof(repository));
      client.ReactionAdded += ReactionAdded;
      client.ReactionRemoved += ReactionRemoved;
    }

    public async Task ReactionAdded(Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction)
    {
      Coin coin = _config.Coins.FirstOrDefault(c => c.EmoteName == reaction.Emote.Name);
      if (coin == null)
        return;

      IUserMessage userMessage = await userMessageProvider.GetOrDownloadAsync();
      if (userMessage.Author.Id == reaction.UserId)
        return;

      float funds = await _service.AddCoin(userMessage.Author.Id, coin);
      Console.WriteLine($"{userMessage.Author.Id} was awarded one {coin.Name} by user with id {reaction.UserId}. New total {funds}");
    }

    public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction)
    {
      Coin coin = _config.Coins.FirstOrDefault(c => c.EmoteName == reaction.Emote.Name);
      if (coin == null)
        return;

      IUserMessage userMessage = await userMessageProvider.GetOrDownloadAsync();
      if (userMessage.Author.Id == reaction.UserId)
        return;

      float funds = await _service.RemoveCoin(userMessage.Author.Id, coin);
      Console.WriteLine($"{userMessage.Author.Id} lost one {coin.Name} because user with id {reaction.UserId} revoked it. New total {funds}");
    }
  }
}
