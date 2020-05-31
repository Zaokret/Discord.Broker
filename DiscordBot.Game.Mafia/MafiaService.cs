using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Game.Mafia
{
    public class MafiaService
    {
        private readonly DiscordSocketClient _client;

        public MafiaService(DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        enum TeamType
        {
            Werewolf,
            Villager
        }

        enum SpecialRole
        {
            Werewolf,
            Villager,
            Seer
        }

        class Player
        {
            public Player(IUser user, TeamType team, SpecialRole role)
            {
                User = user;
                Team = team;
                Role = role;
            }
            public IUser User { get; set; }
            public TeamType Team { get; set; }
            public SpecialRole Role { get; set; }
            public bool Alive { get; set; }
        }

        class GameObject
        {
            public GameObject(List<IUser> users)
            {
                Players = new List<Player>();
                Random random = new Random(Guid.NewGuid().GetHashCode());

                int werewolfOneIndex = random.Next(users.Count);
                Players.Add(new Player(users[werewolfOneIndex], TeamType.Werewolf, SpecialRole.Werewolf));
                users.RemoveAt(werewolfOneIndex);

                int werewolfTwoIndex = random.Next(users.Count);
                Players.Add(new Player(users[werewolfTwoIndex], TeamType.Werewolf, SpecialRole.Werewolf));
                users.RemoveAt(werewolfTwoIndex);

                int seerIndex = random.Next(users.Count);
                Players.Add(new Player(users[seerIndex], TeamType.Werewolf, SpecialRole.Werewolf));
                users.RemoveAt(seerIndex);

                Players = Players.Concat(users.Select(u => new Player(u, TeamType.Villager, SpecialRole.Villager))).ToList();
            }
            public ICollection<Player> Players { get; set; }
            public IReadOnlyCollection<Player> Werewolfs => new ReadOnlyCollection<Player>(Players.Where(p => p.Team == TeamType.Werewolf).ToList());
            public IReadOnlyCollection<Player> Villagers => new ReadOnlyCollection<Player>(Players.Where(p => p.Team == TeamType.Villager).ToList());
            public Player Seer => Players.FirstOrDefault(p => p.Role == SpecialRole.Seer);
        }

        public async Task StartGame(SocketCommandContext context, PendingGame pendingGame)
        {
            // SETUP
            SocketGuild guild = GetGuildByContext(context);
            GameObject game = new GameObject(pendingGame.Users.ToList());
            RestTextChannel village = await CreatePrivateGroupTextChannel(
                guild, 
                "Village", 
                game.Players.Select(p => p.User));

            RestTextChannel den = await CreatePrivateGroupTextChannel(
                guild, 
                "Werewolf Den", 
                game.Werewolfs.Select(p => p.User));

            // NOTIFY - roles, explain rules, win conditions
            await Task.WhenAll(game.Players.Select(p =>
            {
                return p.User.SendMessageAsync($"You are a {p.Role}.");
            }));

            // GAME LOOP
            bool winnerFound = false;
            while(!winnerFound)
            {

            }
        }

        /*
         
        player - create command: created pending game
        player - join command: created village, werewolf den, notified of roles and rules
        player/village - start command ( all players must be ready )
        
        player/village - command vote to lynch someone
        player/den - command vote to kill someone
        player/direct message - command vote to investigate someone
        player/village/village - command vote for someone's role

        bot/village - sunset ( 10 minutes max ):
            command werewolfes transform and pick a target
            command detective picks a target

        bot/village - sunrise ( 10 minutes max )

        every time someone is lynched or killed evaluate win condition
            same number villagers and werewolfs ?
            no more werewolfs ?
        */

        /*
            create werewolf den private dm chat ( unlocked during the night )
            create villige council private dm chat ( unlocked during the day )
            private dm detective

            game loop

                night
                    warewolfs vote who to kill ( $kill @someone )
                day
                    players vote who to lynch ( $lynch @someone )

         */

        private SocketGuild GetGuildByContext(SocketCommandContext context)
        {
            if (context.Guild?.Id != null)
            {
                return _client.GetGuild(context.Guild.Id);
            }
            else if (context.Channel?.Id != null)
            {
                return _client.Guilds.FirstOrDefault(g => g.Channels.All(c => context.Channel.Id == c.Id));
            }
            else
            {
                return _client.Guilds.FirstOrDefault(g => g.Users.Any(u => u.Id == context.User.Id));
            }
        }

        private async Task LockChannelForUsers(RestTextChannel channel, IEnumerable<IUser> users)
        {
            await Task.WhenAll(users.Select(user =>
            {
                return channel.AddPermissionOverwriteAsync(user, BasicViewChannelPerms(channel));
            }));
        }

        private async Task UnlockChannelForUsers(RestTextChannel channel, IEnumerable<IUser> users)
        {
            await Task.WhenAll(users.Select(user =>
            {
                return channel.AddPermissionOverwriteAsync(user, InteractChannelPerms(channel));
            }));
        }

        private OverwritePermissions BasicViewChannelPerms(RestTextChannel channel)
        {
            return OverwritePermissions.DenyAll(channel).Modify(
                    connect: PermValue.Allow,
                    viewChannel: PermValue.Allow,
                    readMessageHistory: PermValue.Allow);
        }

        private OverwritePermissions InteractChannelPerms(RestTextChannel channel)
        {
            return BasicViewChannelPerms(channel).Modify(
                    sendMessages: PermValue.Allow,
                    addReactions: PermValue.Allow,
                    sendTTSMessages: PermValue.Allow);
        }

        private async Task<RestTextChannel> CreatePrivateGroupTextChannel(SocketGuild guild, string name, IEnumerable<IUser> users)
        {
            var everyoneRole = guild.Roles.FirstOrDefault(r => r.Name == "@everyone");
            RestTextChannel channel = await guild.CreateTextChannelAsync(name);
            await channel.AddPermissionOverwriteAsync(everyoneRole, OverwritePermissions.DenyAll(channel));
            await Task.WhenAll(users.Select(user =>
            {
                return channel.AddPermissionOverwriteAsync(user, BasicViewChannelPerms(channel));
            }));
            return channel;
        }

    }
}
