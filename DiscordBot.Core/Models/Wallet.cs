using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class Wallet
    {
        public Wallet(float startingFunds)
        {
            Funds = startingFunds;
        }
        public float Funds { get; set; }

        public Wallet Deposit(float amount)
        {
            Funds += amount;
            return this;
        }

        public Wallet Widthdraw(float amount)
        {
            if (CanWithdraw(amount))
            {
                Funds -= amount;
            }
            return this;
        }

        public bool CanWithdraw(float amount)
        {
            return (Funds - amount) >= 0;
        }

        public float Empty()
        {
            Widthdraw(Funds);
            return Funds;
        }

        public override string ToString()
        {
            return $"{Funds} funds available";
        }
    }
}
