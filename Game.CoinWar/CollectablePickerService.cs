using Discord;
using Discord.WebSocket;
using DiscordBot.Core.Models;
using DiscordBot.Game.CoinWar.Models;
using DiscordBot.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.CoinWar
{
    public class CollectablePickerService
    {
        private readonly CollectableRepository _repository;
        private readonly DiscordSocketClient _client;

        public CollectablePickerService(CollectableRepository repository, DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        private readonly CollectableEntity DefaultCollectable = new CollectableEntity
        {
            ItemName = "tiger",
            Title = "zookeeper",
            EmoteName = "🐅"
        };

        public CollectableEntity GetRandomCollectable()
        {
            List<CollectableEntity> collectables = _repository.GetAll().ToList();

            if(collectables.Count == 0)
            {
                return DefaultCollectable;
            }

            Random random = new Random(Guid.NewGuid().GetHashCode());
            int index = random.Next(collectables.Count);

            Optional<CollectableEntity> chosen = OptionalUtilities.Try(collectables[index]);
            if(!chosen.IsSpecified)
            {
                return DefaultCollectable;
            }
            
            return UpdateEmoteName(chosen.Value);
        }

        private CollectableEntity UpdateEmoteName(CollectableEntity collectable)
        {
            GuildEmote emote = _client.Guilds.SelectMany(g => g.Emotes).FirstOrDefault(e => e.Name == collectable.EmoteName);
            if(emote == null)
            {
                return DefaultCollectable;
            }
            collectable.EmoteName = EmoteStringFormat(emote);
            return collectable;
        }

        private string EmoteStringFormat(GuildEmote emote) => $"<:{emote.Name}:{emote.Id}>";
    }
}
