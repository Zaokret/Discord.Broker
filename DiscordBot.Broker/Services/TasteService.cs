using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    public class Taste
    {
        public Similar Similar { get; set; }
    }

    public class Similar
    {
        public IEnumerable<TasteItem> Info { get; set; }
        public IEnumerable<TasteItem> Results { get; set; }
    }

    public class TasteItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string WTeaser { get; set; }
        public string WUrl { get; set; }
        public string YUrl { get; set; }
        public string YID { get; set; }

    }

    public static class TasteType
    {
        public const string MUSIC = "music";
        public const string MOVIE = "movie";
        public const string SHOW = "show";
        public const string PODCAST = "podcast";
        public const string BOOK = "book";
        public const string AUTHOR = "author";
        public const string GAME = "game";
    }

    public class TasteService
    {
        private readonly HttpClient _httpClient;

        private readonly string baseUrl = "https://tastedive.com/api/similar";
        private readonly int limit = 5;
        private readonly int verbose = 1;
        private readonly GlobalConfiguration _config;

        public TasteService(HttpClient httpClient, GlobalConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<IEnumerable<TasteItem>> GetInfo(string name)
        {
            Taste taste = await GetTaste(name);
            if (taste == null)
                return new List<TasteItem>();
            return taste.Similar.Info.Where(ValidTasteItem).Take(limit);
        }

        public bool ValidTasteItem(TasteItem item)
        {
            if(item.Type.ToLower().Contains("unknown"))
            {
                return !string.IsNullOrWhiteSpace(item.WTeaser);
            }
            return true;
        }

        public async Task<IEnumerable<TasteItem>> GetRecommendation(string name)
        {
            Taste taste = await GetTaste(name);
            if (taste == null)
                return new List<TasteItem>();
            return taste.Similar.Results.Where(ValidTasteItem).Take(limit);
        }

        private async Task<Taste> GetTaste(string name)
        {
            string response = await _httpClient.GetStringAsync(GetUrl(name, _config.TasteToken));
            if (string.IsNullOrWhiteSpace(response))
                return null;

            return JsonConvert.DeserializeObject<Taste>(response);
        }

        private string Encode(string s) => System.Web.HttpUtility.UrlEncode(s);

        private string GetUrl(string name, string token) => $"?q={Encode(name)}&k={token}&verbose={verbose}";
    }
}
