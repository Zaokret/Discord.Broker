using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.ConsoleApp
{
    public static class EnvironmentConfiguration
    {
        public static bool IsDevelopment()
        {
            var envs = Environment.GetEnvironmentVariables();
            if(!envs.Contains("Environment"))
            {
                return false;
            }

            return (string)envs["Environment"] == "Development";
        }

        public static bool IsProduction() => !IsDevelopment();
    }
}
