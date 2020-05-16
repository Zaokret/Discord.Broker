using Discord;
using Discord.WebSocket;
using DiscordBot.Contracts;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Actions
{
  public class ReactionController
  {
    private readonly IUserRepository _repository;
    private readonly Configuration _config;
    public ReactionController(IUserRepository repository, Configuration config)
    {
      _config = config ?? throw new ArgumentNullException(nameof(config));
      _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /*
      await _client.Guilds
              .FirstOrDefault(g => g.Id == _config.MainGuild)
              .GetTextChannel(reaction.Channel.Id)
              .SendMessageAsync(msg);
       */

    public async Task ReactionAdded(Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction)
    {
      Coin coin = _config.Coins.FirstOrDefault(c => new Emoji(c.EmoteCode).Equals(reaction.Emote));
      if (coin == null)
        return;

      IUserMessage userMessage = await userMessageProvider.GetOrDownloadAsync();
      //if (userMessage.Author.Id == reaction.UserId)
      //  return;

      await _repository.AddCoin(userMessage.Author.Id, coin.Value);
      await _repository.SaveAsync();
      float funds = await _repository.GetCoinsByUserId(userMessage.Author.Id);
      Console.WriteLine($"{userMessage.Author.Id} was awarded one {coin.Name} by user with id {reaction.UserId}. New total {funds}");
    }

    public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction)
    {
      Coin coin = _config.Coins.FirstOrDefault(c => new Emoji(c.EmoteCode).Equals(reaction.Emote));
      if (coin == null)
        return;

      IUserMessage userMessage = await userMessageProvider.GetOrDownloadAsync();
      //if (userMessage.Author.Id == reaction.UserId)
      //  return;

      await _repository.SubtractCoin(userMessage.Author.Id, coin.Value);
      await _repository.SaveAsync();
      float funds = await _repository.GetCoinsByUserId(userMessage.Author.Id);
      Console.WriteLine($"{userMessage.Author.Id} lost one {coin.Name} because user with id {reaction.UserId} revoked it. New total {funds}");
    }
  }
}
