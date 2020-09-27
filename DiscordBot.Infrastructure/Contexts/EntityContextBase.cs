using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Infrastructure.Contexts
{
    public abstract class EntityContextBase
    {
        public EntityContextBase(string filePath)
        {
            if(!File.Exists(filePath))
            {
                throw new ArgumentException($"File with filepath {filePath} doesn't exist");
            }

            FilePath = filePath;
        }
        private readonly string FilePath;

        private JArray _context;

        private async Task<JArray> GetContext()
        {
            try
            {
                using (StreamReader reader = new StreamReader(FilePath))
                    return JArray.Parse(await reader.ReadToEndAsync());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<JArray> GetUserJsonArray()
        {
            if (_context == null)
            {
                _context = await GetContext();
            }
            return _context;
        }

        public async Task<bool> SaveUserJsonArray()
        {
            try
            {
                using (StreamWriter file = new StreamWriter(FilePath))
                using (JsonTextWriter writer = new JsonTextWriter(file))
                    await _context.WriteToAsync(writer);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
