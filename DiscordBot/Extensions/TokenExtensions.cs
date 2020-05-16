using DiscordBot.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Extensions
{
  public static class TokenExtensions
  {
    public static T GetUserEntityProperty<T>(this JToken token, EntityTokenProperties property)
    {
      return token.SelectToken(Enum.GetName(typeof(Enum), property)).Value<T>();
    }
  }
}
