using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Contracts;
using DiscordBot.Core.Attributes;
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
        private readonly Random rand = new Random();

        public CoinModule(IUserRepository userRepository, CoinService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        [Command("flip")]
        [Summary("Coin flip")]
        public async Task CoinFlip()
        {
            if (rand.Next(0, 2) == 1)
                await ReplyAsync("Heads");
            else
                await ReplyAsync("Tails");
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
        
        public async Task HandleRoles(
            LeaderboardView leaderboard, 
            IEnumerable<RankedUser> newRichMembers, 
            IEnumerable<SocketGuildUser> richMembers, 
            SocketRole rich)
        {
            foreach (var m in newRichMembers)
            {
                var guidUser = Context.Guild?.GetUser(m.User.Id);
                if (guidUser != null)
                {
                    await guidUser.AddRoleAsync(rich);
                    await Task.Delay(1000);
                }
            }

            var oldRichToRemove = richMembers.Where(currently => !leaderboard.TopUsers.Any(c => c.User.Id == currently.Id));
            foreach (var m in oldRichToRemove)
            {
                var guidUser = Context.Guild?.GetUser(m.Id);
                if (guidUser != null)
                {
                    await guidUser.RemoveRoleAsync(rich);
                    await Task.Delay(1000);
                }
            }
        }

        [Command("rich")]
        [Summary("Retreives coins for top 5 users in a leaderboard.")]
        public async Task GetLeaderboard()
        {
            ulong userId = Context.User.Id;
            List<SocketGuildUser> users = Context.Guild?.Users?.ToList() ?? new List<SocketGuildUser>();
            LeaderboardView leaderboard = await _service.GetLeaderboard(userId, users);

            if(leaderboard == null)
            {
                await ReplyAsync("Records are lost. I'll find them, come back later.");
                return;
            }

            SocketRole rich = Context.Guild?.Roles?.FirstOrDefault(role => role.Name == "Rich");
            if(rich != null)
            {
                var richMembers = rich.Members ?? new List<SocketGuildUser>();
                var newRichMembers = leaderboard.TopUsers.Where(newRich => !richMembers.Any(c => c.Id == newRich.User.Id));
                HandleRoles(leaderboard, newRichMembers, richMembers, rich);
            }

            leaderboard.TheInfinite = Context.Client.GetUser(698910396093825065);

            await Context.Channel.SendMessageAsync("", false, EmbedViews.Leaderboard(leaderboard));
        }

        [Command("transfer")]
        [Summary("Transfers specified funds from the issuer to the specified user")]
        public async Task Transfer(int funds, IUser recipient)
        {
            IUser sender = Context.User;
            if(funds < 1)
            {
                await ReplyAsync("Amount must be greater than zero.");
            }
            else if((await _userRepository.GetCoinsByUserIdAsync(sender.Id)) < funds)
            {
                await ReplyAsync("Insufficient funds.");
            }
            else if(recipient == null)
            {
                await ReplyAsync("Recipient was not specified.");
            }
            else
            {
                await _service.RemoveFunds(sender.Id, funds);
                await _service.AddFunds(recipient.Id, funds);
                await Task.WhenAll(new[] {
                    sender.SendMessageAsync($"{funds} coins sent to {recipient.Username}."),
                    recipient.SendMessageAsync($"{funds} coins received from {sender.Username}.")
                }); 
            }
        }
        
        [RequiredBotAuthor]
        [Command("refund")]
        public async Task RefundFunds(int funds, IUser user)
        {
            await _service.AddFunds(user.Id, funds);
            await _service.SaveAsync();
        }

        [RequiredBotAuthor]
        [Command("seize")]
        public async Task SeizeFunds(int funds, IUser user)
        {
            await _service.RemoveFunds(user.Id, funds);
            await _service.SaveAsync();
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
