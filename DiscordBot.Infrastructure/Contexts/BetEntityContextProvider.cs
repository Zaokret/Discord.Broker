using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Infrastructure.Contexts
{
    public class BetEntityContextProvider : EntityContextBase
    {
        public BetEntityContextProvider() : base("bets.json") { }
    }
}
