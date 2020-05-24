using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Game.CoinWar.Exceptions
{
    class GameAbortException : Exception
    {
        public GameAbortException(string message): base(message) { }
    }
}
