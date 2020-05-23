using DiscordBot.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DiscordBot.Infrastructure.Repositories
{
    public class CollectableRepository
    {
        private IEnumerable<CollectableEntity> Collectables = null;

        public CollectableRepository() { }

        public IEnumerable<CollectableEntity> GetAll()
        {
            if(Collectables != null)
            {
                return Collectables;
            }
            else
            {
                using (StreamReader reader = new StreamReader("collectables.json"))
                {
                    var json = reader.ReadToEnd();
                    Collectables = JsonConvert.DeserializeObject<IEnumerable<CollectableEntity>>(json);
                }
                return Collectables;
            }
            
        }
    }
}
