using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        private readonly DiscordSocketClient _client;

        public CoinModule(IUserRepository userRepository, CoinService service, DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
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
            List<SocketGuildUser> users = Context.Guild?.Users?.ToList() ?? new List<SocketGuildUser>();
            LeaderboardView leaderboard = await _service.GetLeaderboard(userId, users);
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
