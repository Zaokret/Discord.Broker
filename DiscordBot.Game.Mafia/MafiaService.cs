using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Core.Utilities;
using DiscordBot.Game.Mafia.Models;
using DiscordBot.Game.Mafia.Views;
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
        private readonly CoinService _coin;
        private readonly PollService _poll;

        private GameObject ActiveGame;

        private RestTextChannel DayChannel;
        private RestTextChannel NightChannel;
        private RestTextChannel CommandChannel;
        private ICollection<GameChannel> GameChannels = new List<GameChannel>();

        private ICollection<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>> VoteMonitors =
                new List<Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>>();

        private TimeSpan DefaultPhaseCounter = TimeSpan.FromMinutes(10);
        private TimeSpan VisionPhaseCounter = TimeSpan.FromMinutes(4);
        private ICollection<GamePhase> Phases = new List<GamePhase>();

        private const string YES = "👍";

        public MafiaService(DiscordSocketClient client, CoinService coin, PollService poll)
        {
            _poll = poll ?? throw new ArgumentNullException(nameof(poll));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _coin = coin ?? throw new ArgumentNullException(nameof(coin));
        }

        #region Create/Dispose resources
        
        public async Task InitialiseGame(SocketCommandContext context, PendingGame pendingGame)
        {
            ActiveGame = new GameObject(pendingGame.Users.ToList());
            await CreateChannels(context, pendingGame);
            await SendGameRules();
            await InitiateReadyPoll();
            await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
        }

        public async Task CreateChannels(SocketCommandContext context, PendingGame pendingGame)
        {
            SocketGuild guild = GetGuildByContext(context);

            DayChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Public(),
                GameElement.ChannelDescription.Public());

            NightChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Private(),
                GameElement.ChannelDescription.Private(ActiveGame.Uninformed.ToList()));

            CommandChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Command(),
                string.Empty);

            GameChannels = new List<GameChannel>(new[]
            {
                new GameChannel(DayChannel, new [] { "excommunicate" }),
                new GameChannel(NightChannel, new [] { "sacrifice" }),
                new GameChannel(CommandChannel, new [] { "start", "kill", "kick", "signs" })
            });
        }

        public async Task SendGameRules()
        {
            foreach (Embed rule in GameRules.Of())
            {
                await DayChannel.SendMessageAsync(string.Empty, false, rule);
            }
        }

        public async Task InitiateReadyPoll()
        {
            Poll readyPoll = _poll.CreatePoll(GameElement.Poll.Ready(), new List<string>(), _client.CurrentUser);
            IUserMessage message = await DayChannel.SendMessageAsync(string.Empty, false, readyPoll.Message);
            await message.AddReactionsAsync(readyPoll.Emojis.Select(e => new Emoji(e)).ToArray());
            StartMonitoring(CreateMonitor(message.Id, null, MonitorType.Ready));
        }

        private async Task EndGameWithWinner(GroupType winningGroup)
        {
            List<PlayerReward> players = await RewardPlayers(Phases.ToList(), winningGroup);
            await DayChannel.SendMessageAsync(GameEndView.Message(winningGroup));
            await DayChannel.SendMessageAsync(string.Empty, false, GameEndView.Of(winningGroup, players));
            await Dispose();
        }

        private async Task Dispose()
        {
            await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Select(s => s.User));
            await DayChannel.SendMessageAsync("Senate floor splits in two as an earthquake shakes the ground. All are permitted to chat until the senate is destroyed ( 2 minutes ).");
            RemoveMonitors();
            await Task.WhenAll(new[]
            {
                CommandChannel.DeleteAsync(),
                NightChannel.DeleteAsync()
            });
            GameChannels.Clear();
            Phases.Clear();
            PendingGameService.PendingGames.Clear();
            ActiveGame = null;
            await Task.Delay(TimeSpan.FromMinutes(2));
            await DayChannel.DeleteAsync();
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

        public bool IsInvestigator(ulong userId)
        {
            return ActiveGame.Investigator.User.Id == userId;
        }

        public bool IsPhase(PhaseType phase)
        {
            return CurrentPhase() == phase;
        }

        public bool IsUserAlive(ulong userId)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId && p.Active);
        }

        public bool IsUserInTeam(ulong userId, GroupType teamType)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId && p.Group == teamType);
        }

        public bool IsUserPlaying(ulong userId)
        {
            return ActiveGame.Players.Any(p => p.User.Id == userId);
        }
        #endregion  

        #region Game loop

        public bool InformedGroupWins(GameObject game)
        {
            return game.Informed.Count(w => w.Active) >= game.Uninformed.Count(v => v.Active);
        }

        public bool UninformedGroupWins(GameObject game)
        {
            return game.Informed.All(w => !w.Active);
        }

        // circular: night -> visions -> day -> night
        
        public async Task StartGame()
        {
            await ChargeCostOfEntry();
            RemoveMonitors();
            await Task.WhenAll(ActiveGame.Players.Select(p =>
            {
                return p.User.SendMessageAsync(GameElement.InitialRoleReveal(p.Role));
            }));

            await DayChannel.SendMessageAsync(GameElement.PhasePhrase(PhaseType.Night));
            await NightChannel.SendMessageAsync(GameElement.PhaseInstruction(PhaseType.Night));

            await UnlockChannelForUsers(NightChannel, ActiveGame.Informed.Select(p => p.User));
            await LockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
        }

        public async Task ResolveNightPhase(IUser lastNightVictim)
        {
            ResolvePhase(lastNightVictim, PhaseType.Night);
            string nightMessage = lastNightVictim != null
                ? GameElement.VictimChosen(lastNightVictim.Username)
                : GameElement.VictimNotChosen();
            await NightChannel.SendMessageAsync(nightMessage);
            await LockChannelForUsers(NightChannel, ActiveGame.Informed.Select(p => p.User));
        }

        class PlayerOption
        {
            public string Emote { get; set; }
            public Player Player { get; set; }
        }

        public async Task StartVisionPhase()
        {
            var seer = ActiveGame.Investigator;
            await DayChannel.SendMessageAsync(GameElement.PhasePhrase(PhaseType.Investigation));
            if (seer.Active || seer.User.Id == Phases.Last().Target.Id)
            {
                await seer.User.SendMessageAsync(GameElement.PhaseInstruction(PhaseType.Investigation));
                await SendInvestigationPoll(seer);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                await CommandChannel.SendMessageAsync(GetCommandByPhaseType(PhaseType.Investigation));
            }
        }

        private async Task SendInvestigationPoll(Player seer)
        {
            IEnumerable<Player> activePlayers = ActiveGame.Players
                    .Where(p => (p.Active || p.User.Id == Phases.Last().Target.Id) && p.Role != GameRole.Investigator);

            List<string> voteOptions = activePlayers
                .Select(p => p.User.Username)
                .ToList();

            Poll investigationPoll = _poll.CreatePoll(GameElement.Poll.Investigate(), voteOptions, seer.User);
            IUserMessage message = await seer.User.SendMessageAsync(string.Empty, false, investigationPoll.Message);

            List<PlayerOption> options = investigationPoll.Emojis
                .Zip(activePlayers, (emote, player) => new PlayerOption { Emote = emote, Player = player })
                .ToList();
            StartMonitoring(CreateOptionsMonitor(message.Id, options, MonitorType.Investigate));
            await message.AddReactionsAsync(investigationPoll.Emojis.Select(e => new Emoji(e)).ToArray());
        }

        public async Task ResolveVisionPhase(IUser targetUser)
        {
            ResolvePhase(targetUser, PhaseType.Investigation);
            Player investigator = ActiveGame.Investigator;
            if (investigator.Active)
            {
                if (targetUser != null)
                {
                    Player targetPlayer = ActiveGame.Players.FirstOrDefault(p => p.User.Id == targetUser.Id);
                    await investigator.User.SendMessageAsync(GameElement.SeenTheSigns(targetPlayer));
                }
                else
                {
                    await investigator.User.SendMessageAsync(GameElement.NoSigns());
                }
            }
        }

        public async Task StartDayPhase()
        {
            var lastNightVictim = Phases.LastOrDefault(p => p.Phase == PhaseType.Night)?.Target;
            await DayChannel.SendMessageAsync(GameElement.PhasePhrase(PhaseType.Day));
            string dayMessage = lastNightVictim != null
                    ? GameElement.MurderScene(lastNightVictim.Username)
                    : GameElement.NoMurder();

            await DayChannel.SendMessageAsync(dayMessage);

            if(InformedGroupWins(ActiveGame))
            {
                await EndGameWithWinner(GroupType.Informed);
            }
            else
            {
                await DayChannel.SendMessageAsync(GameElement.PhaseInstruction(PhaseType.Day));
                await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Where(p => p.Active).Select(p => p.User));
            }
        }

        public async Task ResolveDayPhase(IUser userToRemove)
        {
            ResolvePhase(userToRemove, PhaseType.Day);
            string dayMessage = userToRemove != null
                ? GameElement.PlayerRemoved(userToRemove.Username)
                : GameElement.NoPlayerRemoved();
            await DayChannel.SendMessageAsync(dayMessage);

            await LockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
        }

        public async Task StartNightPhase()
        {
            var userToRemove = Phases.LastOrDefault(p => p.Phase == PhaseType.Day)?.Target;

            if (UninformedGroupWins(ActiveGame))
            {
                await EndGameWithWinner(GroupType.Uninformed);
            }
            else if (InformedGroupWins(ActiveGame))
            {
                await EndGameWithWinner(GroupType.Informed);
            }
            else
            {
                await DayChannel.SendMessageAsync(GameElement.PhasePhrase(PhaseType.Night));
                await NightChannel.SendMessageAsync(GameElement.PhaseInstruction(PhaseType.Night));
                await UnlockChannelForUsers(NightChannel, ActiveGame.Informed.Where(p => p.Active).Select(p => p.User));
            }
        }
        
        public async Task StartSacrificePoll(IUser target, IUser author)
        {
            Poll killPoll = _poll.CreatePoll(GameElement.Poll.Kill(target.Username), new List<string>(), author);
            IUserMessage message = await NightChannel.SendMessageAsync(string.Empty, false, killPoll.Message);
            await message.AddReactionsAsync(killPoll.Emojis.Select(e => new Emoji(e)).ToArray());
            StartMonitoring(CreateMonitor(message.Id, target, MonitorType.Kill));
        }

        public async Task StartExcommunicatePoll(IUser target, IUser author)
        {
            Poll kickPoll = _poll.CreatePoll(GameElement.Poll.Kick(target.Username), new List<string>(), author);
            IUserMessage message = await DayChannel.SendMessageAsync(string.Empty, false, kickPoll.Message);
            await message.AddReactionsAsync(kickPoll.Emojis.Select(e => new Emoji(e)).ToArray());
            StartMonitoring(CreateMonitor(message.Id, target, MonitorType.Kick));
        }

        private void ResolvePhase(IUser target, PhaseType phase)
        {
            RemoveMonitors();
            if (target != null)
            {
                RemoveUserFromPlay(target);
            }
            Phases.Add(new GamePhase() { Target = target, Phase = phase });
        }

        public void RemoveUserFromPlay(IUser user, GameObject game = null)
        {
            game = game ?? ActiveGame;
            Player victimPlayer = game.Players.FirstOrDefault(p => p.User.Id == user.Id);
            game.Players.Remove(victimPlayer);
            victimPlayer.Active = false;
            game.Players.Add(victimPlayer);
        }
        
        public async Task NotifyPlayerLeft(IUser user)
        {
            await DayChannel.SendMessageAsync($"{user.Username} left the game.");
        }

        #endregion

        #region Phases

        private string GetCommandByPhaseType(PhaseType phase)
        {
            if (phase == PhaseType.Day)
            {
                return "$remove";
            }
            else if (phase == PhaseType.Night)
            {
                return "$kill";
            }
            else if (phase == PhaseType.Investigation)
            {
                return "$signs";
            }
            else
            {
                throw new NotImplementedException($"{nameof(phase)} : {phase}");
            }
        }

        public async Task StartPhaseCounter(PhaseType phase)
        {
            int currentPhaseCount = Phases.Count;
            await Task.Delay(phase == PhaseType.Investigation
                ? VisionPhaseCounter
                : DefaultPhaseCounter);

            // Execute only we stay on the same phase after the phase counter 
            if (currentPhaseCount == Phases.Count)
            {
                await CommandChannel.SendMessageAsync(GetCommandByPhaseType(phase));
            }
        }

        private PhaseType CurrentPhase()
        {
            if (Phases.Count == 0)
            {
                return PhaseType.Night;
            }
            return (PhaseType)(((int)Phases.Last().Phase + 1) % 3);
        }

        #endregion

        #region Vote Monitoring

        private int GetPlayerCountByMonitorType(MonitorType monitor)
        {
            int playerCount = 0;
            switch (monitor)
            {
                case MonitorType.Kill:
                    playerCount = ActiveGame.Informed.Count; break;
                case MonitorType.Kick:
                    playerCount = ActiveGame.Players.Count / 2 + 1; break;
                case MonitorType.Ready:
                    playerCount = ActiveGame.Players.Count; break;
                default:
                    throw new NotImplementedException($"{nameof(monitor)} : {monitor}");
            }
            return playerCount;
        }

        private string CommandByMonitorType(IUser user, MonitorType type)
        {
            string command = string.Empty;
            switch(type)
            {
                case MonitorType.Ready: command = "$start"; break;
                case MonitorType.Kill: command = $"$kill {Mention.Of(user.Id)}"; break;
                case MonitorType.Kick: command = $"$kick {Mention.Of(user.Id)}"; break;
                case MonitorType.Investigate: command = $"$signs {Mention.Of(user.Id)}"; break;
                default:
                    throw new NotImplementedException($"{nameof(type)} : {type}");
            }
            return command;
        }

        Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
        CreateOptionsMonitor(ulong messageId, List<PlayerOption> options, MonitorType monitorType, int voteCount = 1)
        {
            return async (Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                if (reaction.MessageId == messageId)
                {
                    IUserMessage message = await userMessageProvider.GetOrDownloadAsync();
                    int playerCount = voteCount + 1;
                    ReactionSummary reactionSummary = message.Reactions
                        .Select(r => new ReactionSummary(r.Key.Name, r.Value.ReactionCount))
                        .FirstOrDefault(r => r.Count == playerCount);

                    if (reactionSummary != null)
                    {
                        PlayerOption pickedOption = options.FirstOrDefault(o => o.Emote == reactionSummary.Name);
                        await CommandChannel.SendMessageAsync(CommandByMonitorType(pickedOption.Player.User, monitorType));
                    }
                }
            };
        }

        Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
        CreateMonitor(ulong messageId, IUser targetUser, MonitorType monitorType)
        {
            return async (Cacheable<IUserMessage, ulong> userMessageProvider, ISocketMessageChannel channel, SocketReaction reaction) =>
            {
                if (reaction.MessageId == messageId)
                {
                    IUserMessage message = await userMessageProvider.GetOrDownloadAsync();
                    int playerCount = GetPlayerCountByMonitorType(monitorType) + 1;
                    ReactionSummary reactionSummary = message.Reactions
                        .Select(r => new ReactionSummary(r.Key.Name, r.Value.ReactionCount))
                        .FirstOrDefault(r => r.Name == YES && r.Count == playerCount);

                    if (reactionSummary != null)
                    {
                        await CommandChannel.SendMessageAsync(CommandByMonitorType(targetUser, monitorType));
                    }
                }
            };
        }

        private void RemoveMonitors()
        {
            foreach (var monitor in VoteMonitors)
            {
                _client.ReactionAdded -= monitor;
            }
            VoteMonitors.Clear();
        }

        private void StartMonitoring(
            Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> voteMonitor)
        {
            _client.ReactionAdded += voteMonitor;
            VoteMonitors.Add(voteMonitor);
        }
        #endregion

        #region Channel

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

        private async Task<bool> LockChannelForUsers(RestTextChannel channel, IEnumerable<IUser> users, int retries = 10)
        {
            if (retries == 0)
            {
                return false;
            }
            OverwritePermissions perms = BasicViewChannelPerms(channel);
            await Task.WhenAll(users.Select(user =>
            {
                return channel.AddPermissionOverwriteAsync(user, perms);
            }));
            IEnumerable<IUser> brokenUsers = UsersWithIncorrectPermissions(perms, channel.Id, users);
            if (brokenUsers.Any())
            {
                return await LockChannelForUsers(channel, brokenUsers, retries - 1);
            }
            return true;
        }

        private async Task<bool> UnlockChannelForUsers(RestTextChannel channel, IEnumerable<IUser> users, int retries = 10)
        {
            if(retries == 0)
            {
                return false;
            }
            OverwritePermissions perms = InteractChannelPerms(channel);
            await Task.WhenAll(users.Select(user =>
            {
                return channel.AddPermissionOverwriteAsync(user, perms);
            }));
            IEnumerable<IUser> brokenUsers = UsersWithIncorrectPermissions(perms, channel.Id, users);
            if(brokenUsers.Any())
            {
                return await UnlockChannelForUsers(channel, brokenUsers, retries - 1);
            }
            return true;
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

        private async Task<RestTextChannel> CreatePrivateGroupTextChannel(SocketGuild guild, string name, string description)
        {
            var everyoneRole = guild.Roles.FirstOrDefault(r => r.Name == "@everyone");
            RestTextChannel channel = await guild.CreateTextChannelAsync(name, (properties) => { properties.Topic = description; });
            await channel.AddPermissionOverwriteAsync(everyoneRole, OverwritePermissions.DenyAll(channel));
            return channel;
        }

        private RestTextChannel ChannelByGroupType(GroupType group)
        {
            return group == GroupType.Informed
                ? NightChannel
                : DayChannel;
        }
        
        private bool ArePermissionsEqual(OverwritePermissions a, OverwritePermissions b)
        {
            var bList = b.ToAllowList();
            return a.ToAllowList().All(permA => bList.Contains(permA));
        }

        private IEnumerable<IUser> UsersWithIncorrectPermissions(
            OverwritePermissions correctPerms, 
            ulong channelId, 
            IEnumerable<IUser> users)
        {
            SocketTextChannel channel = _client.Guilds.SelectMany(g => g.TextChannels).FirstOrDefault(c => c.Id == channelId);
            return users.Where(user =>
            {
                if (channel.PermissionOverwrites.All(overwrite => overwrite.TargetId != user.Id))
                    return true;
                Overwrite playerOverwrite = channel.PermissionOverwrites.First(overwrite => overwrite.TargetId == user.Id);
                return !ArePermissionsEqual(playerOverwrite.Permissions, correctPerms);
            });
        }

        /*private OverwritePermissions GetPermissionsByPhaseAndChannel(PhaseType phase, RestTextChannel channel, GroupType group)
        {
            if(channel.Name == GameElement.Channel.Public())
            {
                if(phase == PhaseType.Day)
                {
                    return InteractChannelPerms(channel);
                }
                else
                {
                    return BasicViewChannelPerms(channel);
                }
            }
            else if(channel.Name == GameElement.Channel.Private())
            {
                if(phase == PhaseType.Night && group == GroupType.Informed)
                {
                    return InteractChannelPerms(channel);
                }
                else
                {
                    return BasicViewChannelPerms(channel);
                }
            }
            else
            {
                throw new NotImplementedException(channel.Name);
            }
        }*/

        /*private async Task FixPlayersPermissions(List<Player> players)
        {
            PhaseType phase = CurrentPhase();
            List<Player> playersWithWrongPermissions = players.Where(player =>
            {
                RestTextChannel channel = ChannelByGroupType(player.Group);
                return channel.PermissionOverwrites.Any(perm =>
                {
                    OverwritePermissions rightPerms = GetPermissionsByPhaseAndChannel(phase, channel, player.Group);
                    return perm.TargetId == player.User.Id && !ArePermissionsEqual(perm.Permissions, rightPerms);
                });
            }).ToList();

            if(playersWithWrongPermissions.Count > 0)
            {
                await Task.WhenAll(playersWithWrongPermissions.Select(player =>
                {
                    RestTextChannel channel = ChannelByGroupType(player.Group);
                    OverwritePermissions rightPerms = GetPermissionsByPhaseAndChannel(phase, channel, player.Group);
                    return channel.AddPermissionOverwriteAsync(player.User, rightPerms);
                }));
                await FixPlayersPermissions(players);
            }
        }*/

        #endregion

        #region Coins

        public class PlayerReward
        {
            public Player Player { get; set; }
            public int Reward { get; set; }
            public int DaysAliveFor { get; set; }
        }

        private async Task<List<PlayerReward>> RewardPlayers(List<GamePhase> phases, GroupType winningGroup)
        {
            List<PlayerReward> rewards = GetPlayerRewards(ActiveGame, phases, ActiveGame.Players, winningGroup);
            await Task.WhenAll(rewards.Select(r =>
            {
                return _coin.AddFunds(r.Player.User.Id, r.Reward);
            }));
            await _coin.SaveAsync();
            return rewards;
        }

        private List<PlayerReward> GetPlayerRewards(GameObject game, ICollection<GamePhase> phases, ICollection<Player> players, GroupType winningGroup)
        {
            List<Player> informed = game.Informed.ToList();
            List<Player> uninformed = game.Uninformed.ToList();
            List<GamePhase> dayAndNightPhases = phases
                .Where(p => p.Phase == PhaseType.Day || p.Phase == PhaseType.Night)
                .ToList();

            return players.Select(player =>
            {
                int countOfAlivePhases = dayAndNightPhases
                    .FindIndex(phase => phase.Target != null && phase.Target.Id == player.User.Id);

                if (countOfAlivePhases == -1)
                {
                    countOfAlivePhases = dayAndNightPhases.Count();
                }

                List<GamePhase> alivePhases = dayAndNightPhases
                    .Take(countOfAlivePhases)
                    .ToList();

                int daysAlive = (int)Math.Floor((alivePhases.Count / 2f));
                int rewardForStayingAlive = daysAlive * PriceConfiguration.CoinsPerLivedRound;

                int additionalReward = player.Group == GroupType.Informed
                    ? GetInformedPlayerReward(player, alivePhases, uninformed)
                    : GetUninformedPlayerReward(player, alivePhases, informed);

                int survivalBonus = player.Active ? PriceConfiguration.CoinsForSurvival : 0;

                int coins = rewardForStayingAlive + additionalReward + survivalBonus;
                float winMultiplier = player.Group == winningGroup ? PriceConfiguration.WinCoinMultiplier : 1;

                int reward = (int)Math.Ceiling(winMultiplier * coins);
                return new PlayerReward
                {
                    Player = player,
                    Reward = reward,
                    DaysAliveFor = daysAlive
                };
            }).ToList();
        }

        private int GetUninformedPlayerReward(Player player, List<GamePhase> alivePhases, List<Player> informed)
        {
            int count = alivePhases.Count(phase => phase.Target != null && informed.Any(i => i.User.Id == phase.Target.Id));
            return count * PriceConfiguration.CoinsPerKickOfInformedMember;
        }

        private int GetInformedPlayerReward(Player player, List<GamePhase> alivePhases, List<Player> uninformed)
        {
            Player investigator = uninformed.FirstOrDefault(p => p.Role == GameRole.Investigator);
            return alivePhases
                .Where(phase => phase.Target != null && uninformed.Any(i => i.User.Id == phase.Target.Id))
                .Aggregate(0, (total, phase) =>
                {
                    float multiplier = investigator.User.Id == phase.Target.Id ? PriceConfiguration.InvestigatorKillMultiplayer : 1;
                    int reward = (int)Math.Ceiling(PriceConfiguration.CoinsPerKill * multiplier);
                    return total + reward;
                });
        }

        private async Task ChargeCostOfEntry()
        {
            await Task.WhenAll(ActiveGame.Players.Select(p =>
            {
                return _coin.RemoveFunds(p.User.Id, PriceConfiguration.CostOfEntry);
            }));
            await _coin.AddFunds(_client.CurrentUser.Id, PriceConfiguration.CostOfEntry * ActiveGame.Players.Count);
            await _coin.SaveAsync();
        }

        #endregion

        #region Tests

        public async Task PlayerRewardsTest()
        {
            IEnumerable<IUser> users = _client.Guilds.SelectMany(g => g.Users).Take(8);
            GameObject game = new GameObject(users.ToList());
            Random random = new Random(Guid.NewGuid().GetHashCode());
            ICollection<GamePhase> gamePhases = new List<GamePhase>();
            while (!InformedGroupWins(game) && !InformedGroupWins(game))
            {
                List<Player> activePlayers = game.Players.Where(p => p.Active).ToList();
                Player playerToBeRemoved = activePlayers[random.Next(activePlayers.Count)];
                RemoveUserFromPlay(playerToBeRemoved.User, game);
                gamePhases.Add(new GamePhase
                {
                    Target = playerToBeRemoved.User,
                    Phase = playerToBeRemoved.Group == GroupType.Informed ? PhaseType.Day : PhaseType.Night
                });
            }
            GroupType winers = InformedGroupWins(game) ? GroupType.Informed : GroupType.Uninformed;
            List<PlayerReward> rewards = GetPlayerRewards(game, gamePhases, game.Players.ToList(), winers);
            IUser me = _client.Guilds.SelectMany(g => g.Users).FirstOrDefault(u => u.Username == "JJ 3maj");
            await me.SendMessageAsync(string.Empty, false, GameEndView.Of(winers, rewards));
        }

        #endregion 
    }
}
