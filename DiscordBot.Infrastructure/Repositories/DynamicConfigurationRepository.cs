using DiscordBot.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Infrastructure.Repositories
{
    public class DynamicConfigurationRepository
    {
        public static string Filepath = "dynamic-config.json";
        public DynamicConfigurationRepository() { }

        public DynamicConfiguration Get()
        {
            using (StreamReader reader = new StreamReader(Filepath))
            {
                return JsonConvert.DeserializeObject<DynamicConfiguration>(reader.ReadToEnd());
            }
        }

        public bool Update(DynamicConfiguration config)
        {
            try
            {
                using (StreamWriter file = new StreamWriter(Filepath))
                using (JsonTextWriter writer = new JsonTextWriter(file))
                    JObject.FromObject(config).WriteTo(writer);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
