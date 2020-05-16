using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
  interface ILogger<T>
  {
    Task Log(LogMessage msg);
  }

  class Logger<T> : ILogger<T>
  {
    public static Task Log(LogMessage msg)
    {
      Console.WriteLine($"{typeof(T)} {msg.ToString()}");
      return Task.CompletedTask;
    }

    Task ILogger<T>.Log(LogMessage msg)
    {
      return Logger<T>.Log(msg);
    }
  }
}
