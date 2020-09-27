using DiscordBot.Core.Models;
using DiscordBot.Infrastructure.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Infrastructure.Repositories
{
    enum BetEntityProperties {
        Name
    }

    public class BetRepository
    {
        private readonly BetEntityContextProvider _contextProvider;

        public BetRepository(BetEntityContextProvider contextProvider)
        {
            _contextProvider = contextProvider ?? throw new ArgumentException(nameof(contextProvider));
        }

        public async Task<IEnumerable<Bet>> GetAll(Func<Bet, bool> predicate)
        {
            var bets = await GetAllAsync();
            return bets.Where(predicate);
        }

        public async Task<Bet> GetBetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            var bets = await GetAllAsync();
            return bets.FirstOrDefault(b => 
                !string.IsNullOrWhiteSpace(b.Name) && 
                b.Name.ToLower() == name.ToLower().Trim());
        }

        public async Task<Bet> GetBetByOptionId(int betOptionId)
        {
            var bets = await GetAllAsync();
            return bets.FirstOrDefault(bet => bet.Options.Any(opt => opt.Id == betOptionId));
        }

        public async Task<IEnumerable<Bet>> GetBetsByUserId(ulong userId)
        {
            var bets = await GetAllAsync();
            return bets.Where(bet => bet.Bettors.Any(bettor => bettor.UserId == userId));
        }

        public async Task<Bet> AddBet(Bet bet)
        {
            var bets = await GetJArrayAsync();
            bets.Add(JToken.FromObject(bet));
            return bet;
        }

        public async Task UpdateBet(Bet bet)
        {
            var bets = await GetJArrayAsync();
            bets.Select(b =>
            {
                if (ByToken(BetEntityProperties.Name, bet.Name)(b))
                {

                    b.Replace(JToken.FromObject(bet));
                }
                return b;
            }).ToList();
        }

        public async Task SaveAsync()
        {
            await _contextProvider.SaveUserJsonArray();
        }

        public async Task<IEnumerable<Bet>> GetAllAsync()
        {
            return (await _contextProvider.GetUserJsonArray()).ToObject<IEnumerable<Bet>>();
        }

        public async Task<JArray> GetJArrayAsync()
        {
            return await _contextProvider.GetUserJsonArray();
        }

        private Func<JToken, bool> ByToken<T>(BetEntityProperties prop, T val) where T : IEquatable<T>
        {
            if (!Enum.IsDefined(typeof(BetEntityProperties), prop))
            {
                throw new Exception($"Enum property {prop} is not defined.");
            }
            return t =>
            {
                JToken token = t.SelectToken(Enum.GetName(typeof(BetEntityProperties), prop));
                if (token == null)
                    return false;
                return EqualityComparer<T>.Default.Equals(token.Value<T>(), val);
            };
        }
    }
}
