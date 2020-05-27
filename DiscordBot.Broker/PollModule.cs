using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Broker
{
    [NamedArgumentType]
    public class PollCommandArguments
    {
        public string Description { get; set; }
        public string Title { get; set; }
    }

    // Please note that the library already supports type reading
    // primitive types such as bool. This example is merely used
    // to demonstrate how one could write a simple TypeReader.
    public class ListOfStringTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(new List<string>()));
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(input.Split(',').Select(s => s.Trim()).ToList()));
        }
    }

    public class PollModule : ModuleBase<SocketCommandContext>
    {
        public static List<string> ZeroIndexedEmojiNumbers = new List<string>(new[] 
        {
            "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"
        });

        public PollModule() { }

        [Command("poll")]
        [Summary("Creates a poll.")]
        public async Task GetCoinsAsync(List<string> options, PollCommandArguments args)
        {
            Optional<string> errorMessage = ValidatePollCommandArguments(args, options);
            if(errorMessage.IsSpecified)
            {
                await ReplyAsync(errorMessage.Value);
                
            }
            else
            {
                Poll poll = CreatePoll(args, options, Context.User);
                IUserMessage pollMessage = await ReplyAsync("", false, poll.Message);
                await pollMessage.AddReactionsAsync(poll.Emojis.Select(e => new Emoji(e)).ToArray());
            }
        }

        class VoteOption
        {
            public Emoji Emoji { get; set; }
            public string Description { get; set; }
        }

        class Poll
        {
            public Embed Message { get; set; }
            public ICollection<string> Emojis { get; set; }
        }

        private Poll CreatePoll(PollCommandArguments args, List<string> optionList, IUser author)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithAuthor(author)
                .WithTitle(args.Title)
                .WithDescription(args.Description)
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);

            if(optionList.Count == 0)
            {
                return new Poll
                {
                    Message = builder
                    .AddField("👍", "Yes", true)
                    .AddField("👎", "No", true)
                    .Build(),
                    Emojis = new[] { "👍", "👎" }
                };
            }
            else
            {
                string ops = string.Join("\n\n", optionList.Select((o, i) => $"{ZeroIndexedEmojiNumbers[i]} {o}"));
                List<string> emojis = optionList.Select((o, i) => ZeroIndexedEmojiNumbers[i]).ToList();
                return new Poll
                {
                    Message = builder
                    .AddField("# Options", ops, false)
                    .Build(),
                    Emojis = emojis
                };
            }
        }

        private Optional<string> ValidatePollCommandArguments(PollCommandArguments args, IEnumerable<string> options)
        {
            if (string.IsNullOrWhiteSpace(args.Title))
            {
                return Optional.Create("Title must not be empty.");
            }
            if (string.IsNullOrWhiteSpace(args.Description))
            {

                return Optional.Create("Description must not be empty.");
            }
            if(options.Count() == 1)
            {
                return Optional.Create("Don't provide options for yes/no poll and provide more than one option for custom poll.");
            }
            if(options.Count() > 10)
            {
                return Optional.Create("10 options max.");
            }
            return Optional<string>.Unspecified;
        }

        [Command("poll")]
        [Summary("Creates a poll.")]
        public async Task GetCoinsAsync()
        {
            await ReplyAsync("Create a poll by sending a message [$poll \"optionName1, optionName2, optionName3\" Title: \"Title Text\" Description: \"Description Text\"] Send \"\" instead of options to create a yes/no poll.");
        }
    }
}
