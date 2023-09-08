using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Goatbot.Data;
using Goatbot.Data.Models;
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

    public Bird(DiscordSocketClient client, IConfiguration config, BirdDbContext db)
    {
        _client = client;
        _config = config;
        _db = db;
        _client.MessageReceived += OnMessageAsync;
        client.Ready += ReadyAsync;
        client.UserVoiceStateUpdated += UserVoiceStateUpdatedAsync;
        client.ReactionAdded += ReactionAddedAsync;
        client.ReactionRemoved += ReactionRemovedAsync;
        voidIds = _config.GetSection("VoidIds").Get<ulong[]>();
        if (!string.IsNullOrEmpty(config.GetValue<string>("BirdsPath")))
        {
            var birdsBasePath = config.GetValue<string>("BirdsPath");
            birds = Directory.GetFiles(birdsBasePath).Select(x => Path.Combine(birdsBasePath, x)).ToArray();
        }
    }

    public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> messageCacheable, Cacheable<IMessageChannel, ulong> channelCacheable, SocketReaction reaction)
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
    
    public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> messageCacheable, Cacheable<IMessageChannel, ulong> channelCacheable, SocketReaction reaction)
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
    
    public async Task UserVoiceStateUpdatedAsync(SocketUser socketUser, SocketVoiceState currentState,
        SocketVoiceState nextState)
    {
        if (nextState.VoiceChannel.Id == 801249696571850762)
        {
            if (nextState.IsMuted) return;
            await nextState.VoiceChannel.GetUser(socketUser.Id).ModifyAsync(x => x.Mute = true);
        }

        if (nextState.VoiceChannel == null || nextState.VoiceChannel.Id != 801249696571850762)
        {
            if (!nextState.IsMuted) return;
            await nextState.VoiceChannel.GetUser(socketUser.Id).ModifyAsync(x => x.Mute = false);
        }
    }


    public async Task ReadyAsync()
    {
        // var user = _client.GetUser(310241454603763713);
        // await user.SendMessageAsync("im going to shit on your car");
        // Console.WriteLine("done");
    }

    public async Task OnMessageAsync(SocketMessage message)
    {
        if (voidIds.Contains(message.Channel.Id))
        {
            await message.DeleteAsync();
        }

        if (CheckWordlist(message.Content, new List<string> { "bird", "burb", "birb" }))
        {
            await message.Channel.SendMessageAsync("a");
        }

        if (message.Content.ToLower().Contains("fugl"))
        {
            await message.Channel.SendMessageAsync("Æ");
        }

        if (message.Channel is IVoiceChannel && ((IVoiceChannel)message.Channel).GuildId == 595687467827462144)
            await message.DeleteAsync();

        if (message.Channel.Id == 595999302334021632 && (message.Attachments.Any() || message.Content.Contains("https://") || message.Content.Contains("http://")))
        {
            await message.AddReactionAsync(Emote.Parse("<:upvote:1130557003698290708>"));
        }
    }

    [SlashCommand("bird", "bird")]
        public async Task BirdAsync()
        {
            var bird = random.Next(birds.Length);
            await RespondWithFileAsync(birds[bird]);
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