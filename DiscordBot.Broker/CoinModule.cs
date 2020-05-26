using Discord;
using Discord.Commands;
using DiscordBot.Broker;
using DiscordBot.Contracts;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class CoinModule : ModuleBase<SocketCommandContext>
    {
        private readonly IUserRepository _userRepository;
        private readonly CoinService _service;

        public CoinModule(IUserRepository userRepository, CoinService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        [Command("coins")]
        [Summary("Retreives coins for user or issuer.")]
        public async Task GetCoinsAsync([Summary("User to get coins for.")] IUser user = null)
        {
            user = user ?? Context.User;
            if (!(await _userRepository.UserExistAsync(user.Id)))
            {
                await ReplyAsync($"Fella is off the books.");
            }
            else
            {
                float coins = await _userRepository.GetCoinsByUserIdAsync(user.Id);
                int rounded = (int)Math.Round(coins);
                await ReplyAsync(GetUserMessage(rounded, user.Username));
            }
        }

        [Command("rich")]
        [Summary("Retreives coins for top 5 users in a leaderboard.")]
        public async Task GetLeaderboard()
        {
            ulong userId = Context.User.Id;
            List<NamedUser> users = Context.Guild.Users.Select(u => new NamedUser { Id = u.Id, Username = u.Username }).ToList();
            LeaderboardView leaderboard = await _service.GetLeaderboard(userId, users);
            await Context.Channel.SendMessageAsync("", false, EmbedViews.Leaderboard(leaderboard));
        }

        [Command("help")]
        [Summary("Send help instructions.")]
        public async Task GetHelp()
        {
            await Context.Channel.SendFileAsync("./assets/guide-to-staying-relaxed.mp3");
        }

        private string GetUserMessage(int coins, string username)
        {
            if (coins == 0)
                return $"{username} is empty-handed.";
            if (coins == 1)
                return $"{username} has one Attarcoin.";

            return $"{username} has {coins} Attarcoins.";
        }
    }
}
