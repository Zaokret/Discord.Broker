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
        private readonly GlobalConfiguration _config;
        private readonly CoinService _service;
        private readonly DiscordSocketClient _client;

        public ReactionController(DiscordSocketClient client, IUserRepository repository, GlobalConfiguration config, CoinService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private async Task BrokerAwarded(IUserMessage message, int reactionCount)
        {
            string reply = string.Empty;
            switch(reactionCount)
            {
                case 10:
                    reply = "10 coins, thank you for your contribution.";
                    break;
                case 50:
                    reply = "50 coins, thats a bit much no? Thank you.";
                    break;
                case 100:
                    reply = "100 coins, thats very generous. You're all awasome!";
                    break;

            }

            if(!string.IsNullOrWhiteSpace(reply))
            {
                await message.Channel.SendMessageAsync(reply);
            }
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Coin coin = _config.Coins.FirstOrDefault(c => c.EmoteName == reaction.Emote.Name);
            if (coin == null)
                return;

            IUserMessage userMessage = await userMessageProvider.GetOrDownloadAsync();
            if (userMessage == null || userMessage.Author.Id == reaction.UserId)
                return;
            
            if (userMessage.Author.Id == _client.CurrentUser.Id)
            {
                KeyValuePair<IEmote, ReactionMetadata> reactionMeta = userMessage.Reactions.FirstOrDefault(r => r.Key.Name == coin.Name);
                await BrokerAwarded(userMessage, reactionMeta.Value.ReactionCount);
            }

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
