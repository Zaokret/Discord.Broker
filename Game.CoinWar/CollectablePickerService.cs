using Discord;
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

        public CollectablePickerService(CollectableRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        private readonly CollectableEntity DefaultCollectable = new CollectableEntity
        {
            Name = "tiger",
            Title = "zookeeper",
            Emote = "🐯"
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
            return chosen.Value;
        }
    }
}
