﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Goatbot.Data;
using Goatbot.Data.Models;
using Goatbot.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Modules;

public class Bird : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ulong[] voidIds;
    private readonly string[] birds;
    private readonly BirdDbContext _db;
    private static Random random = new Random();

    private IGuildUser grandon;
    private IGuild hbi;

    private static bool isInitialized = false;

    public Bird(DiscordSocketClient client, IConfiguration config, BirdDbContext db)
    {
        _client = client;
        _config = config;
        _db = db;
        if (isInitialized) return;
        isInitialized = true;
        _client.MessageReceived += OnMessageAsync;
        client.Ready += ReadyAsync;
        client.ReactionAdded += ReactionAddedAsync;
        client.ReactionRemoved += ReactionRemovedAsync;
        voidIds = _config.GetSection("VoidIds").Get<ulong[]>();
        if (!string.IsNullOrEmpty(config.GetValue<string>("BirdsPath")))
        {
            var birdsBasePath = config.GetValue<string>("BirdsPath");
            birds = Directory.GetFiles(birdsBasePath).Select(x => Path.Combine(birdsBasePath, x)).ToArray();
        }
    }

    public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> messageCacheable,
        Cacheable<IMessageChannel, ulong> channelCacheable, SocketReaction reaction)
    {
        var message = await messageCacheable.GetOrDownloadAsync();
        if (channelCacheable.Id == 595999302334021632 && message.Attachments.Any() &&
            Equals(reaction.Emote, Emote.Parse("<:upvote:1130557003698290708>")) &&
            reaction.UserId != _client.CurrentUser.Id && reaction.UserId != message.Author.Id)
        {
            var user = await _db.Upvotes.SingleAsync(x => x.User == message.Author.Id);
            user.Count--;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> messageCacheable,
        Cacheable<IMessageChannel, ulong> channelCacheable, SocketReaction reaction)
    {
        var message = await messageCacheable.GetOrDownloadAsync();
        if (channelCacheable.Id == 595999302334021632 && message.Attachments.Any() &&
            Equals(reaction.Emote, Emote.Parse("<:upvote:1130557003698290708>")) &&
            reaction.UserId != _client.CurrentUser.Id)
        {
            if (reaction.UserId == message.Author.Id)
            {
                await message.RemoveReactionAsync(reaction.Emote, reaction.UserId);
            }
            else
            {
                if (await _db.Upvotes.AnyAsync(x => x.User == message.Author.Id))
                {
                    var user = await _db.Upvotes.SingleAsync(x => x.User == message.Author.Id);
                    user.Count++;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var user = new Upvotes
                    {
                        User = message.Author.Id,
                        Count = 1
                    };
                    await _db.Upvotes.AddAsync(user);
                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }

    public async Task ReadyAsync()
    {
        // var user = _client.GetUser(310241454603763713);
        // await user.SendMessageAsync("im going to shit on your car");
        // Console.WriteLine("done");

        hbi = _client.GetGuild(595687467827462144);
        grandon = await hbi.GetUserAsync(269605239756161025);

        Task.Run(WhatShouldIcallThisGoddamnMethodIDontKnowLol);
    }

    public async Task WhatShouldIcallThisGoddamnMethodIDontKnowLol()
    {
        while (true)
        {
            await grandon.ModifyAsync(x => x.Nickname = _db.Gs.OrderBy(y => EF.Functions.Random()).Take(1).ToList()[0].Word.FirstCharToUpper());
            await Task.Delay(86400000);
        }
    }

    public async Task OnMessageAsync(SocketMessage message)
    {
        if (voidIds.Contains(message.Channel.Id))
        {
            await message.DeleteAsync();
        }

        if ((message.Content.StartsWith("*kicks") || message.Content.StartsWith("kicks")) &&
            message.MentionedUsers.Select(x => x.Id).Contains(_client.CurrentUser.Id))
        {
            message.Channel.SendMessageAsync("*dies*");
        }

        if (CheckWordlist(message.Content, new List<string> { "bird", "burb", "birb" }))
        {
            if (random.Next(100) == 69)
            {
                await message.Channel.SendMessageAsync("bcdefghijklmnopqurstuvwxyz".ToCharArray()[random.Next(25)].ToString());
                await Task.Delay(1000);
                await message.Channel.SendMessageAsync("I mean");
                await Task.Delay(500);
                await message.Channel.SendMessageAsync("fuck");
                await Task.Delay(500);
                await message.Channel.SendMessageAsync("a");
            }
            else
            {
                await message.Channel.SendMessageAsync("a");
            }
        }

        if (message.Content.ToLower().Contains("fugl"))
        {
            await message.Channel.SendMessageAsync("Æ");
        }

        if (message.Channel is IVoiceChannel && ((IVoiceChannel)message.Channel).GuildId == 595687467827462144)
            await message.DeleteAsync();

        if (message.Channel.Id == 595999302334021632 && (message.Attachments.Any() ||
                                                         message.Content.Contains("https://") ||
                                                         message.Content.Contains("http://")))
        {
            await message.AddReactionAsync(Emote.Parse("<:upvote:1130557003698290708>"));
        }

        if (message.Content.ToLower().Contains("car"))
        {
           // await message.AddReactionAsync(new Emoji("\uD83D\uDCA9"));
        }
     }

    [SlashCommand("bird", "bird")]
    public async Task BirdAsync()
    {
        var bird = random.Next(birds.Length);
        await RespondWithFileAsync(birds[bird]);
    }

    [SlashCommand("g", "g")]
    public async Task GAsync(string word)
    {
        if (!word.StartsWith("g") && !word.StartsWith("G"))
        {
            await RespondAsync("Must start with g dumbass");
            return;
        }

        if (_db.Gs.Any(x => x.Word == word.ToLower()))
        {
            await RespondAsync("I already have that one tho");
            return;
        }

        await _db.Gs.AddAsync(new g { Word = word.ToLower() });
        await _db.SaveChangesAsync();
        await RespondAsync("k");
    }
    
    private bool CheckWordlist(string input, IEnumerable<string> wordlist)
    {
        foreach (var word in wordlist)
        {
            if (input.ToLower().Contains(word)) return true;
        }

        return false;
    }

    [SlashCommand("memes", "get the memes leaderboard")]
    public async Task MemesAsync()
    {
        EmbedBuilder builder = new EmbedBuilder();
        builder.WithTitle("Meme Leaderboard");
        await _db.Upvotes.OrderByDescending(x => x.Count)
            .ForEachAsync(async x =>
            {
                var user = await _client.GetUserAsync(x.User);
                builder.AddField(user.Username, x.Count.ToString());
            });
        await RespondAsync(embed: builder.Build());
    }
}