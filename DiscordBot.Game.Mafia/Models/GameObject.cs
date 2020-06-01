using Discord;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DiscordBot.Game.Mafia.Models
{
    public class GameObject
    {
        public GameObject(List<IUser> users)
        {
            Players = new List<Player>();
            Random random = new Random(Guid.NewGuid().GetHashCode());

            int firstInformed = random.Next(users.Count);
            Players.Add(new Player(users[firstInformed], GroupType.Informed, GameRole.Killer));
            users.RemoveAt(firstInformed);

            int secondInformed = random.Next(users.Count);
            Players.Add(new Player(users[secondInformed], GroupType.Informed, GameRole.Killer));
            users.RemoveAt(secondInformed);

            int investigator = random.Next(users.Count);
            Players.Add(new Player(users[investigator], GroupType.Uninformed, GameRole.Investigator));
            users.RemoveAt(investigator);

            Players = Players.Concat(users.Select(u => new Player(u, GroupType.Uninformed, GameRole.Kicker))).ToList();
        }

        public ICollection<Player> Players { get; set; }
        public IReadOnlyCollection<Player> Informed => new ReadOnlyCollection<Player>(Players.Where(p => p.Group == GroupType.Informed).ToList());
        public IReadOnlyCollection<Player> Uninformed => new ReadOnlyCollection<Player>(Players.Where(p => p.Group == GroupType.Uninformed).ToList());
        public Player Investigator => Players.FirstOrDefault(p => p.Role == GameRole.Investigator);
    }
}
