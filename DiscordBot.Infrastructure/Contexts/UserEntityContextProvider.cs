using DiscordBot.Infrastructure.Contexts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Contexts
{
    public class UserEntityContextProvider : EntityContextBase
    { 
        public UserEntityContextProvider() : base("users.json") { }
    }
}
