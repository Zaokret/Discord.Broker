using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class User
    {
        public User() { }
        public User(IUser user, Wallet wallet)
        {
            Wallet = wallet;
            DiscordUser = user;
        }
        public Wallet Wallet { get; set; }
        public IUser DiscordUser { get; set; }
        public override string ToString()
        {
            return $"User with id {DiscordUser.Id} has wallet {Wallet}.";
        }
    }
}
