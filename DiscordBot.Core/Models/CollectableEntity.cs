using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DiscordBot.Core.Models
{
    public class CollectableEntity
    {
        public string EmoteName { get; set; }
        public string ItemName { get; set; }
        public string Title { get; set; }
        public string Win { get; set; }
        public string Lose { get; set; }
    }
}
