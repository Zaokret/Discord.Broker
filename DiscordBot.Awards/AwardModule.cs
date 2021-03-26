﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Broker;
using DiscordBot.Core.Extensions;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Awards
{
    public class AwardModule : ModuleBase<SocketCommandContext>
    {
        private readonly CoinService _coinService;
        private readonly AwardService _awardService;
        private readonly GlobalConfiguration _config;

        public AwardModule(CoinService coinService, AwardService awardService, GlobalConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _coinService = coinService ?? throw new ArgumentNullException(nameof(coinService));
            _awardService = awardService ?? throw new ArgumentNullException(nameof(awardService));
        }

        [Command("award")]
        [Summary("Award a post")]
        public async Task GiveAward()
        {
            var funds = await _coinService.GetFundsByUserId(Context.User.Id);

            if(funds >= _config.AwardCoinAmount)
            {
                if (Context.Message.Reference != null && Context.Message.Reference.MessageId.IsSpecified)
                {
                    try
                    {
                        var award = await _awardService.GetAward(Context.Message, Context.User);
                        if (award.IsSpecified)
                        {
                            var awardsChannel = Context.Guild.GetTextChannel(_config.AwardsChannelID);
                            var awardMessage = await awardsChannel.SendMessageAsync(string.Empty, false, award.Value);
                            await _awardService.Transfer(Context.User.Id, Context.Message.Author.Id, _config.AwardCoinAmount);
                            await Context.Message.Author.SendMessageAsync($"{Context.User.Username} awarded you with {_config.AwardCoinAmount} coins for your post <{award.Value.Url}>");
                        }
                        else
                        {
                            await ReplyAsync("Can't '$award' your own post.");
                        }
                    }
                    catch
                    {
                        await ReplyAsync("There was an issue with awarding this post.");
                    }
                }
                else
                {
                    await ReplyAsync("Issue '$award' command in a reply to a post.");
                }
            }
            else
            {
                await ReplyAsync($"You don't have {_config.AwardCoinAmount} coins to '$award' this post.");
            }
        }

        [Command("pin")]
        [Summary("Award a post")]
        public async Task PinPost()
        {
            var funds = await _coinService.GetFundsByUserId(Context.User.Id);

            if (funds >= _config.PinCoinAmount)
            {
                if (Context.Message.Reference != null && Context.Message.Reference.MessageId.IsSpecified)
                {
                    try
                    {
                        var award = await _awardService.GetAward(Context.Message, Context.User);
                        if (award.IsSpecified)
                        {
                            var awardsChannel = Context.Guild.GetTextChannel(_config.AwardsChannelID);
                            var awardMessage = await awardsChannel.SendMessageAsync(string.Empty, false, award.Value);
                            await awardMessage.PinAsync();
                            await _awardService.Transfer(Context.User.Id, Context.Message.Author.Id, _config.PinCoinAmount);
                            await Context.Message.Author.SendMessageAsync($"{Context.User.Username} awarded you with {_config.PinCoinAmount} coins for your post <{award.Value.Url}>");
                        }
                        else
                        {
                            await ReplyAsync("Can't '$pin' your own post.");
                        }
                    }
                    catch
                    {
                        await ReplyAsync("There was an issue with pinning this post.");
                    }
                }
                else
                {
                    await ReplyAsync("Issue '$pin' command in a reply to a post.");
                }
            }
            else
            {
                await ReplyAsync($"You don't have {_config.PinCoinAmount} coins to '$pin' this post.");
            }
        }
    }
}
