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
        private GameObject ActiveGame;
        private RestTextChannel DayChannel;
        private RestTextChannel NightChannel;
        private RestTextChannel CommandChannel;
        private ICollection<GameChannel> GameChannels = new List<GameChannel>();
        private ICollection<VoteMonitor> VoteMonitors =
                new List<VoteMonitor>();
        private TimeSpan DefaultPhaseCounter = TimeSpan.FromMinutes(10);
        private TimeSpan VisionPhaseCounter = TimeSpan.FromMinutes(4);
        private ICollection<GamePhase> Phases = new List<GamePhase>();

        public MafiaService(DiscordSocketClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #region Phases

        public async Task StartPhaseCounter(Phase phase)
        {
            int currentPhaseCount = Phases.Count;
            await Task.Delay(phase == Phase.Vision 
                ? VisionPhaseCounter
                : DefaultPhaseCounter);

            // Execute only we stay on the same phase after the phase counter 
            if(currentPhaseCount == Phases.Count)
            {
                if (phase == Phase.Day)
                {
                    await CommandChannel.SendMessageAsync("$remove");
                }
                else if (phase == Phase.Night)
                {
                    await CommandChannel.SendMessageAsync("$kill");
                }
                else if (phase == Phase.Vision)
                {
                    await CommandChannel.SendMessageAsync("$vision");
                }
                else
                {
                    throw new NotImplementedException($"{nameof(phase)} : {phase}");
                }
            }
        }

        public enum Phase
        {
            Night,
            Vision,
            Day
        }

        class GamePhase
        {
            public Phase Phase { get; set; }
            public IUser Target { get; set; }
        }

        private Phase CurrentPhase()
        {
            if(Phases.Count == 0)
            {
                return Phase.Night;
            }
            return (Phase)(((int)Phases.Last().Phase + 1) % 3);
        }

        #endregion

        #region Vote Monitoring

        class VoteOption
        {
            public VoteOption(string emoteName, ulong userId)
            {
                EmoteName = emoteName;
                UserId = userId;
            }

            public VoteOption(string emoteName) : this(emoteName, default(ulong)) { }

            public string EmoteName { get; set; }
            public ulong UserId { get; set; }
        }

        class VoteMonitor
        {
            public VoteMonitor(
                Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> monitor,
                ICollection<VoteOption> voteOptions)
            {
                Monitor = monitor;
                VoteOptions = voteOptions;
            }
            public Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> Monitor { get; set; }
            public ICollection<VoteOption> VoteOptions { get; set; }
        }

        enum MonitorType
        {
            Remove,
            Kill,
            Ready
        }

        private int GetPlayerCountByMonitorType(MonitorType monitor)
        {
            int playerCount = 0;
            switch (monitor)
            {
                case MonitorType.Kill:
                    playerCount = ActiveGame.Werewolfs.Count; break;
                case MonitorType.Remove:
                    playerCount = ActiveGame.Players.Count / 2 + 1; break;
                case MonitorType.Ready:
                    playerCount = ActiveGame.Players.Count; break;
                default:
                    throw new NotImplementedException($"{nameof(monitor)} : {monitor}");
            }
            return playerCount;
        }

        class ReactionSummary
        {
            public ReactionSummary(string name, int count)
            {
                Name = name;
                Count = count;
            }
            public string Name { get; set; }
            public int Count { get; set; }
        }

        readonly ICollection<VoteOption> SinglePlayerVote = new List<VoteOption>(new[]
        {
            new VoteOption("yes"),
            new VoteOption("no")
        });

        Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
        CreateReadyMonitor(ulong messageId)
        {
            return async (Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                if (reaction.MessageId == messageId)
                {
                    IUserMessage message = await userMessageProvider.GetOrDownloadAsync();
                    int playerCount = GetPlayerCountByMonitorType(MonitorType.Ready) + 1;
                    ReactionSummary reactionSummary = message.Reactions
                        .Select(r => new ReactionSummary(r.Key.Name, r.Value.ReactionCount))
                        .FirstOrDefault(r => r.Name == "yes" && r.Count == playerCount);

                    if (reactionSummary != null)
                    {
                        await CommandChannel.SendMessageAsync("$start");
                    }
                }
            };
        }

        // GLOBAL UTIL
        private string Mention(ulong userId)
        {
            return $"<@{userId}>";
        }

        Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
        CreateKillMonitor(ulong messageId, IUser targetUser)
        {
            return async (Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                if (reaction.MessageId == messageId)
                {
                    IUserMessage message = await userMessageProvider.GetOrDownloadAsync();
                    int playerCount = GetPlayerCountByMonitorType(MonitorType.Kill) + 1;

                    ReactionSummary reactionSummary = message.Reactions
                        .Select(r => new ReactionSummary(r.Key.Name, r.Value.ReactionCount))
                        .FirstOrDefault(r => r.Name == "yes" && r.Count == playerCount);

                    if (reactionSummary != null)
                    {
                        await CommandChannel.SendMessageAsync($"$kill {Mention(targetUser.Id)}");
                    }
                }
            };
        }

        Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
        CreateRemoveMonitor(ulong messageId, IUser targetUser)
        {
            return async (Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                if (reaction.MessageId == messageId)
                {
                    IUserMessage message = await userMessageProvider.GetOrDownloadAsync();
                    int playerCount = GetPlayerCountByMonitorType(MonitorType.Remove) + 1;

                    ReactionSummary reactionSummary = message.Reactions
                        .Select(r => new ReactionSummary(r.Key.Name, r.Value.ReactionCount))
                        .FirstOrDefault(r => r.Name == "yes" && r.Count == playerCount);

                    if (reactionSummary != null)
                    {
                        await CommandChannel.SendMessageAsync($"$remove {Mention(targetUser.Id)}");
                    }
                }
            };
        }

        private void RemoveMonitors()
        {
            foreach (var monitor in VoteMonitors.Select(vm => vm.Monitor))
            {
                _client.ReactionAdded -= monitor;
            }
            VoteMonitors.Clear();
        }

        private void StartMonitoring(VoteMonitor voteMonitor)
        {
            _client.ReactionAdded += voteMonitor.Monitor;
            VoteMonitors.Add(voteMonitor);
        }
        #endregion

        #region Validation

        public bool IsGameActive()
        {
            return ActiveGame != null;
        }

        public bool IsCommandValid(string commandName, ulong channelId)
        {
            return GameChannels.Any(c => c.Channel.Id == channelId && c.Commands.Contains(commandName));
        }

        public bool IsSeer(ulong userId)
        {
            return ActiveGame.Seer.User.Id == userId;
        }

        public bool IsPhase(Phase phase)
        {
            return CurrentPhase() == phase;
        }

        public bool IsUserAlive(ulong userId)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId && p.Alive);
        }

        public bool IsUserInTeam(ulong userId, TeamType teamType)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId && p.Team == teamType);
        }

        public bool IsUserPlaying(ulong userId)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId);
        }
        #endregion  

        #region Game classes
        public enum TeamType
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
        #endregion

        #region Create/Dispose resources

        public async Task InitialiseGame(SocketCommandContext context, PendingGame pendingGame)
        {
            // TODO EXTRACT STRINGS TO CONFIG

            // SETUP
            SocketGuild guild = GetGuildByContext(context);
            ActiveGame = new GameObject(pendingGame.Users.ToList());
            DayChannel = await CreatePrivateGroupTextChannel(
                guild,
                "Village",
                ActiveGame.Players.Select(p => p.User));

            NightChannel = await CreatePrivateGroupTextChannel(
                guild,
                "Werewolf Den",
                ActiveGame.Werewolfs.Select(p => p.User));

            CommandChannel = await CreatePrivateGroupTextChannel(
                guild,
                "command-channel",
                new[] { _client.CurrentUser });

            GameChannels = new List<GameChannel>(new[]
            {
                new GameChannel(DayChannel, new [] { "excommunicate", "role" }),
                new GameChannel(NightChannel, new [] { "sacrifice", "role" }),
                new GameChannel(CommandChannel, new [] { "sunset", "sunrise", "vision", "start", "ready", "kill", "remove" })
            });

            // NOTIFY
            await DayChannel.SendMessageAsync("Game rules.");

            // READY PHASE
            await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
            RestUserMessage readyMessage = await DayChannel.SendMessageAsync("Everyone react to this when you are ready and we start with the night phase.");

            // ADD START HANDLER
            VoteMonitor vm = new VoteMonitor(
                CreateReadyMonitor(readyMessage.Id),
                new List<VoteOption>(new[]
                {
                    new VoteOption("yes"),
                    new VoteOption("no")
                }));
            StartMonitoring(vm);
        }

        private async Task Dispose()
        {
            RemoveMonitors();
            await Task.WhenAll(new[]
            {
                CommandChannel.DeleteAsync(),
                NightChannel.DeleteAsync(),
                DayChannel.DeleteAsync()
            });
            GameChannels.Clear();
            Phases.Clear();
            ActiveGame = null;
        }

        #endregion

        #region Game loop

        // circular: night -> visions -> day -> night

        public async Task StartGame()
        {
            RemoveMonitors();
            await Task.WhenAll(ActiveGame.Players.Select(p =>
            {
                return p.User.SendMessageAsync($"You are a {p.Role}.");
            }));

            await LockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
            await DayChannel.SendMessageAsync("Cultists are gathering.");

            await UnlockChannelForUsers(NightChannel, ActiveGame.Werewolfs.Select(p => p.User));
            await NightChannel.SendMessageAsync("Vote who to sacrifice");
        }

        public async Task ResolveNightPhase(IUser lastNightVictim)
        {
            ResolvePhase(lastNightVictim, Phase.Night);

            await LockChannelForUsers(NightChannel, ActiveGame.Werewolfs.Where(p => p.Alive).Select(p => p.User));
            string nightMessage = lastNightVictim != null
                ? $"{lastNightVictim.Username} is the chosen target."
                : "Sunrise is on the horizon and there will be no victims tonight.";
            await NightChannel.SendMessageAsync(nightMessage);
        }

        public async Task StartVisionPhase()
        {
            var seer = ActiveGame.Seer;
            await DayChannel.SendMessageAsync("Taking the auspices.");
            if (seer.Alive)
            {
                await seer.User.SendMessageAsync("Investigate one of the players to see their role. Use command '$vision @someone' ");
            }
        }

        public async Task ResolveVisionPhase(IUser targetUser)
        {
            ResolvePhase(targetUser, Phase.Vision);
            if (targetUser != null)
            {
                Player targetPlayer = ActiveGame.Players.FirstOrDefault(p => p.User.Id == targetUser.Id);
                await ActiveGame.Seer.User.SendMessageAsync($"{targetPlayer.User.Username} is a {targetPlayer.Team}");
            }
            else
            {
                await ActiveGame.Seer.User.SendMessageAsync($"You lost your vision and missed a chance to check someones team.");
            }
        }

        public async Task StartDayPhase()
        {
            var lastNightVictim = Phases.LastOrDefault(p => p.Phase == Phase.Night)?.Target;

            if(ActiveGame.Villagers.Count(v => v.Alive) != ActiveGame.Werewolfs.Count(w => w.Alive))
            {
                await UnlockChannelForUsers(DayChannel, ActiveGame.Villagers.Where(p => p.Alive).Select(p => p.User));
                string dayMessage = lastNightVictim != null
                    ? $"Day is starting. Last night {lastNightVictim.Username} died. Some extra message on the first day."
                    : "Day is starting. Everybody stayed alive last night.";
                await DayChannel.SendMessageAsync(dayMessage);
            }
            else
            {
                await LockChannelForUsers(NightChannel, ActiveGame.Werewolfs.Where(p => p.Alive).Select(p => p.User));
                await DayChannel.SendMessageAsync("Werewolfs win because they reached numerical parity with the villagers.");
                await Dispose();
            }
        }

        public async Task ResolveDayPhase(IUser userToRemove)
        {
            ResolvePhase(userToRemove, Phase.Day);
            await LockChannelForUsers(DayChannel, ActiveGame.Villagers.Where(p => p.Alive).Select(p => p.User));
            string dayMessage = userToRemove != null
                ? $"{userToRemove.Username} was excommunicated."
                : "Day is ending and everybody stays.";
            await DayChannel.SendMessageAsync(dayMessage);
        }

        public async Task StartNightPhase()
        {
            var userToRemove = Phases.LastOrDefault(p => p.Phase == Phase.Day)?.Target;
            if (ActiveGame.Werewolfs.Count(w => w.Alive) > 0)
            {
                await UnlockChannelForUsers(NightChannel, ActiveGame.Werewolfs.Where(p => p.Alive).Select(p => p.User));
                string nightMessage = userToRemove != null
                    ? $"{userToRemove.Username} was excommunicated today. IT WAS ONE OF YOU OR NOT"
                    : "No one was excommunicated today.";
                await NightChannel.SendMessageAsync(nightMessage);
            }
            else
            {
                await LockChannelForUsers(DayChannel, ActiveGame.Villagers.Where(p => p.Alive).Select(p => p.User));
                await DayChannel.SendMessageAsync("Last werewolf was killed last night. Game over.");
                await Dispose();
            }
        }
        
        public async Task StartSacrificePoll(IUser target)
        {
            IUserMessage killMessage = await NightChannel.SendMessageAsync("We should kill this player.");
            VoteMonitor vm = new VoteMonitor(CreateKillMonitor(killMessage.Id, target), SinglePlayerVote);
            StartMonitoring(vm);
        }

        public async Task StartExcommunicatePoll(IUser target)
        {
            IUserMessage message = await NightChannel.SendMessageAsync("We should remove this player.");
            VoteMonitor vm = new VoteMonitor(CreateRemoveMonitor(message.Id, target), SinglePlayerVote);
            StartMonitoring(vm);
        }

        private void ResolvePhase(IUser target, Phase phase)
        {
            RemoveMonitors();
            if (target != null)
            {
                RemoveUserFromPlay(target);
            }
            Phases.Add(new GamePhase() { Target = target, Phase = phase });
        }

        public void RemoveUserFromPlay(IUser user)
        {
            Player victimPlayer = ActiveGame.Players.FirstOrDefault(p => p.User.Id == user.Id);
            ActiveGame.Players.Remove(victimPlayer);
            victimPlayer.Alive = false;
            ActiveGame.Players.Add(victimPlayer);
        }
        
        public async Task NotifyPlayerLeft(IUser user)
        {
            await DayChannel.SendMessageAsync($"{user.Username} left the game.");
        }

        #endregion

        #region Channel
        
        class GameChannel
        {
            public GameChannel(RestTextChannel channel, IEnumerable<string> commands)
            {
                Channel = channel;
                Commands = commands;
            }

            public RestTextChannel Channel { get; set; }
            public IEnumerable<string> Commands { get; set; }
        }
        
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
                return channel.AddPermissionOverwriteAsync(user, OverwritePermissions.DenyAll(channel));
            }));
            return channel;
        }
        #endregion
    }
}
