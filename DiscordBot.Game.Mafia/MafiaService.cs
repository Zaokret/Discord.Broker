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
        
        public MafiaService(DiscordSocketClient client, CoinService coin)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _coin = coin ?? throw new ArgumentNullException(nameof(coin));
        }

        #region Create/Dispose resources

        public async Task InitialiseGame(SocketCommandContext context, PendingGame pendingGame)
        {
            // TODO EXTRACT STRINGS TO CONFIG

            // SETUP
            SocketGuild guild = GetGuildByContext(context);
            ActiveGame = new GameObject(pendingGame.Users.ToList());
            DayChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Public(),
                ActiveGame.Players.Select(p => p.User));

            NightChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Private(),
                ActiveGame.Informed.Select(p => p.User));

            CommandChannel = await CreatePrivateGroupTextChannel(
                guild,
                GameElement.Channel.Command(),
                new[] { _client.CurrentUser });

            GameChannels = new List<GameChannel>(new[]
            {
                new GameChannel(DayChannel, new [] { "excommunicate" }),
                new GameChannel(NightChannel, new [] { "sacrifice" }),
                new GameChannel(CommandChannel, new [] { "start", "kill", "kick" })
            });

            // NOTIFY
            await DayChannel.SendMessageAsync("Game rules.");

            // READY PHASE
            await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
            RestUserMessage readyMessage = await DayChannel.SendMessageAsync("Everyone react to this when you are ready and we start with the night phase.");

            // ADD START HANDLER
            StartMonitoring(CreateMonitor(readyMessage.Id, null, MonitorType.Ready));
        }

        private async Task EndGameWithWinner(GroupType winningGroup)
        {
            await RewardPlayers();
            await Dispose();
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
            PendingGameService.PendingGames.Clear();
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

        // circular: night -> visions -> day -> night
        
        public async Task StartGame()
        {
            await ChargeCostOfEntry();
            RemoveMonitors();
            await Task.WhenAll(ActiveGame.Players.Select(p =>
            {
                return p.User.SendMessageAsync($"Your role is {p.Role}.");
            }));

            await LockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
            await DayChannel.SendMessageAsync("Cultists are gathering.");

            await UnlockChannelForUsers(NightChannel, ActiveGame.Informed.Select(p => p.User));
            await NightChannel.SendMessageAsync("Vote who to sacrifice");
        }

        public async Task ResolveNightPhase(IUser lastNightVictim)
        {
            ResolvePhase(lastNightVictim, PhaseType.Night);

            await LockChannelForUsers(NightChannel, ActiveGame.Informed.Select(p => p.User));
            string nightMessage = lastNightVictim != null
                ? $"{lastNightVictim.Username} is the chosen target."
                : "Sunrise is on the horizon and there will be no victims tonight.";
            await NightChannel.SendMessageAsync(nightMessage);
        }

        public async Task StartVisionPhase()
        {
            var seer = ActiveGame.Investigator;
            await DayChannel.SendMessageAsync("Taking the auspices.");
            if (seer.Active)
            {
                await seer.User.SendMessageAsync("Investigate one of the players to see their role. Use command '$vision @someone' ");
            }
        }

        public async Task ResolveVisionPhase(IUser targetUser)
        {
            ResolvePhase(targetUser, PhaseType.Investigation);
            if (targetUser != null)
            {
                Player targetPlayer = ActiveGame.Players.FirstOrDefault(p => p.User.Id == targetUser.Id);
                await ActiveGame.Investigator.User.SendMessageAsync($"{targetPlayer.User.Username} is a {targetPlayer.Group}");
            }
            else
            {
                await ActiveGame.Investigator.User.SendMessageAsync($"You lost your vision and missed a chance to check someones team.");
            }
        }

        public async Task StartDayPhase()
        {
            var lastNightVictim = Phases.LastOrDefault(p => p.Phase == PhaseType.Night)?.Target;

            if(ActiveGame.Uninformed.Count(v => v.Active) == ActiveGame.Informed.Count(w => w.Active))
            {
                await EndGameWithWinner(GroupType.Informed);
            }
            else
            {
                await UnlockChannelForUsers(DayChannel, ActiveGame.Players.Where(p => p.Active).Select(p => p.User));
                string dayMessage = lastNightVictim != null
                    ? $"Day is starting. Last night {lastNightVictim.Username} died. Some extra message on the first day."
                    : "Day is starting. Everybody stayed alive last night.";
                await DayChannel.SendMessageAsync(dayMessage);
            }
        }

        public async Task ResolveDayPhase(IUser userToRemove)
        {
            ResolvePhase(userToRemove, PhaseType.Day);
            await LockChannelForUsers(DayChannel, ActiveGame.Players.Select(p => p.User));
            string dayMessage = userToRemove != null
                ? $"{userToRemove.Username} was excommunicated."
                : "Day is ending and everybody stays.";
            await DayChannel.SendMessageAsync(dayMessage);
        }

        public async Task StartNightPhase()
        {
            var userToRemove = Phases.LastOrDefault(p => p.Phase == PhaseType.Day)?.Target;

            if (ActiveGame.Informed.Count(w => w.Active) == 0)
            {
                await EndGameWithWinner(GroupType.Uninformed);
            }
            else if(ActiveGame.Uninformed.Count(v => v.Active) == ActiveGame.Informed.Count(w => w.Active))
            {
                await EndGameWithWinner(GroupType.Informed);
            }
            else
            {
                await UnlockChannelForUsers(NightChannel, ActiveGame.Informed.Where(p => p.Active).Select(p => p.User));
                string nightMessage = userToRemove != null
                    ? $"{userToRemove.Username} was excommunicated today. IT WAS ONE OF YOU OR NOT"
                    : "No one was excommunicated today.";
                await NightChannel.SendMessageAsync(nightMessage);
            }
        }
        
        public async Task StartSacrificePoll(IUser target)
        {
            IUserMessage killMessage = await NightChannel.SendMessageAsync("We should kill this player.");
            StartMonitoring(CreateMonitor(killMessage.Id, target, MonitorType.Kill));
        }

        public async Task StartExcommunicatePoll(IUser target)
        {
            IUserMessage message = await NightChannel.SendMessageAsync("We should remove this player.");
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

        public void RemoveUserFromPlay(IUser user)
        {
            Player victimPlayer = ActiveGame.Players.FirstOrDefault(p => p.User.Id == user.Id);
            ActiveGame.Players.Remove(victimPlayer);
            victimPlayer.Active = false;
            ActiveGame.Players.Add(victimPlayer);
        }
        
        public async Task NotifyPlayerLeft(IUser user)
        {
            await DayChannel.SendMessageAsync($"{user.Username} left the game.");
        }

        #endregion

        #region Phases

        public async Task StartPhaseCounter(PhaseType phase)
        {
            int currentPhaseCount = Phases.Count;
            await Task.Delay(phase == PhaseType.Investigation
                ? VisionPhaseCounter
                : DefaultPhaseCounter);

            // Execute only we stay on the same phase after the phase counter 
            if (currentPhaseCount == Phases.Count)
            {
                if (phase == PhaseType.Day)
                {
                    await CommandChannel.SendMessageAsync("$remove");
                }
                else if (phase == PhaseType.Night)
                {
                    await CommandChannel.SendMessageAsync("$kill");
                }
                else if (phase == PhaseType.Investigation)
                {
                    await CommandChannel.SendMessageAsync("$vision");
                }
                else
                {
                    throw new NotImplementedException($"{nameof(phase)} : {phase}");
                }
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
                case MonitorType.Kick: command = $"kick {Mention.Of(user.Id)}"; break;
                default:
                    throw new NotImplementedException($"{nameof(type)} : {type}");
            }
            return command;
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
                        .FirstOrDefault(r => r.Name == "yes" && r.Count == playerCount);

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

        #region Coins

        class PlayerReward
        {
            public Player Player { get; set; }
            public int Reward { get; set; }
        }

        private async Task RewardPlayers()
        {
            List<PlayerReward> rewards = GetPlayerRewards(Phases.ToList(), ActiveGame.Players);
            await Task.WhenAll(rewards.Select(r =>
            {
                return _coin.AddFunds(r.Player.User.Id, r.Reward);
            }));
            await _coin.SaveAsync();
        }

        private List<PlayerReward> GetPlayerRewards(ICollection<GamePhase> phases, ICollection<Player> players)
        {
            List<Player> informed = ActiveGame.Informed.ToList();
            List<Player> uninformed = ActiveGame.Uninformed.ToList();
            List<GamePhase> dayAndNightPhases = phases
                .Where(p => p.Phase == PhaseType.Day || p.Phase == PhaseType.Night)
                .ToList();

            return players.Select(player =>
            {
                int countOfAlivePhases = dayAndNightPhases
                    .FindIndex(phase => phase.Target != null && phase.Target.Id == player.User.Id);

                List<GamePhase> alivePhases = dayAndNightPhases
                    .Take(countOfAlivePhases)
                    .ToList();

                int rewardForStayingAlive = alivePhases.Count * PriceConfiguration.CoinsPerLivedRound;

                int additionalReward = player.Group == GroupType.Informed
                    ? GetUninformedPlayerReward(player, alivePhases, informed)
                    : GetUninformedPlayerReward(player, alivePhases, uninformed);

                return new PlayerReward
                {
                    Player = player,
                    Reward = rewardForStayingAlive + additionalReward
                };
            }).ToList();
        }

        private int GetUninformedPlayerReward(Player player, List<GamePhase> alivePhases, List<Player> informed)
        {
            int count = alivePhases.Count(phase => phase.Target != null && informed.Any(i => i.User.Id == phase.Target.Id));
            return count * PriceConfiguration.CoinsPerKickOfInformedMember;
        }

        private float GetInformedPlayerReward(Player player, List<GamePhase> alivePhases, List<Player> uninformed)
        {
            Player investigator = ActiveGame.Investigator;
            return alivePhases
                .Where(phase => phase.Target != null && uninformed.Any(i => i.User.Id == phase.Target.Id))
                .Aggregate(0, (total, phase) =>
                {
                    int multiplier = investigator.User.Id == phase.Target.Id ? PriceConfiguration.InvestigatorKillMultiplayer : 1;
                    return total + PriceConfiguration.CoinsPerKill * multiplier;
                });
        }

        private async Task ChargeCostOfEntry()
        {
            // TODO Implement coin transfer
            await Task.WhenAll(ActiveGame.Players.Select(p =>
            {
                return _coin.RemoveFunds(p.User.Id, PriceConfiguration.CostOfEntry);
            }));
            await _coin.AddFunds(_client.CurrentUser.Id, PriceConfiguration.CostOfEntry * ActiveGame.Players.Count);
            await _coin.SaveAsync();
        }

        #endregion
    }
}
