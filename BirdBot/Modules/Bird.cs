using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Goatbot.Data;
using Goatbot.Data.Models;
using Goatbot.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YoutubeDLSharp;

namespace Goatbot.Modules;

public class Bird : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly string[] mcDeaths = ["<player> was pricked to death","<player> walked into a cactus while trying to escape <player/mob>","<player> drowned","<player> drowned while trying to escape <player/mob>","<entity> died from dehydration","<entity> died from dehydration while trying to escape <player/mob>","<player> experienced kinetic energy","<player> experienced kinetic energy while trying to escape <player/mob>","<player> blew up","<player> was blown up by <player/mob>","<player> was blown up by <player/mob> using <item>","<player> hit the ground too hard","<player> hit the ground too hard while trying to escape <player/mob>","<player> fell from a high place","<player> fell off a ladder","<player> fell off some vines","<player> fell off some weeping vines","<player> fell off some twisting vines","<player> fell off scaffolding","<player> fell while climbing","death.fell.accident.water","<player> was doomed to fall","<player> was doomed to fall by <player/mob>","<player> was doomed to fall by <player/mob> using <item>","<player> was impaled on a stalagmite","<player> was impaled on a stalagmite while fighting <player/mob>","<player> was squashed by a falling anvil","<player> was squashed by a falling block","<player> was skewered by a falling stalactite","<player> went up in flames","<player> walked into fire while fighting <player/mob>","<player> burned to death","<player> was burned to a crisp while fighting <player/mob>","<player> went off with a bang","<player> went off with a bang due to a firework fired from <item> by <player/mob>","<player> tried to swim in lava","<player> tried to swim in lava to escape <player/mob>","<player> was struck by lightning","<player> was struck by lightning while fighting <player/mob>","<player> discovered the floor was lava","<player> walked into the danger zone due to <player/mob>","<player> was killed by magic","<player> was killed by magic while trying to escape <player/mob>","<player> was killed by <player/mob> using magic","<player> was killed by <player/mob> using <item>","<player> froze to death","<player> was frozen to death by <player/mob>","<player> was slain by <player/mob>","<player> was slain by <player/mob> using <item>","<player> was stung to death","<player> was stung to death by <player/mob> using <item>","<player> was obliterated by a sonically-charged shriek","<player> was obliterated by a sonically-charged shriek while trying to escape <player/mob> wielding <item>","<player> was smashed by <player/mob>","<player> was smashed by <player/mob> with <item>","<player> was speared by <player/mob>","<player> was speared by <player/mob> using <item>","<player> was shot by <player/mob>","<player> was shot by <player/mob> using <item>","<player> was pummeled by <player/mob>","<player> was pummeled by <player/mob> using <item>","<player> was fireballed by <player/mob>","<player> was fireballed by <player/mob> using <item>","<player> was shot by a skull from <player/mob>","<player> was shot by a skull from <player/mob> using <item>","<player> starved to death","<player> starved to death while fighting <player/mob>","<player> suffocated in a wall","<player> suffocated in a wall while fighting <player/mob>","<player> was squished too much","<player> was squashed by <player/mob>","<player> left the confines of this world","<player> left the confines of this world while fighting <player/mob>","<player> was poked to death by a sweet berry bush","<player> was poked to death by a sweet berry bush while trying to escape <player/mob>","<player> was killed while trying to hurt <player/mob>","<player> was killed by <item> while trying to hurt <player/mob>","<player> was impaled by <player/mob>","<player> was impaled by <player/mob> with <item>","<player> fell out of the world","<player> didn't want to live in the same world as <player/mob>","<player> withered away","<player> withered away while fighting <player/mob>","<player> died","<player> died because of <player/mob>","<player> was killed","<player> was killed while fighting <player/mob>","<player> was killed by even more magic","<player>"];
    
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ulong[] voidIds;
    private readonly string[] birds;
    private readonly BirdDbContext _db;
    private static Random random = new Random();

    private static readonly Regex EmojiRegex = new Regex(
        @"\p{Cs}|\p{C}|\uD83C[\uDF00-\uDFFF]|\uD83D[\uDC00-\uDE4F\uDE80-\uDEFF]|[\u2600-\u26FF\u2700-\u27BF]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private IGuildUser grandon;
    private IGuild hbi;

    private static bool isInitialized = false;

    private static async Task<IMessage> GetMessageToActOnForWakeWord(IMessage message)
    {
        var refMessage = message.Reference?.MessageId.ToNullable();
        if (refMessage.HasValue) return await message.Channel.GetMessageAsync(refMessage.Value); 
        return (await message.Channel.GetMessagesAsync(message.Id, Direction.Before, 4).FlattenAsync()).First((x) => !x.Author.IsBot);
    }

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
            //birds = Directory.GetFiles(birdsBasePath).Select(x => Path.Combine(birdsBasePath, x)).ToArray();
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
            await grandon.ModifyAsync(x =>
                x.Nickname = _db.Gs.OrderBy(y => EF.Functions.Random()).Take(1).ToList()[0].Word.FirstCharToUpper());
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
            await message.Channel.SendMessageAsync("*dies*");
        }

        if (message.Content.ToLower().Replace(",", "").Contains("bird") && message.Content.ToLower().Contains("react"))
        {
            Emoji? emojiToReact = null;
            
            // This is to get rid of the first instance of the word "bird". This is because its just looking for the word "bird", 
            // And if it is found, it will use the first valid emoji. Since the word bird is also an emoji we don't want to include it in the check
            // Unless it's the second occurence (like "bird, bird react this man")
            string messageContentWithoutWakeWord = message.Content;
            int indexOfWakeWord = messageContentWithoutWakeWord.IndexOf("bird", StringComparison.CurrentCultureIgnoreCase);
            messageContentWithoutWakeWord = messageContentWithoutWakeWord.Substring(0, indexOfWakeWord) + messageContentWithoutWakeWord.Substring(indexOfWakeWord + "bird".Length);
           
            foreach (var word in messageContentWithoutWakeWord.Split(' '))
            {
                var success = Emoji.TryParse(word, out emojiToReact);
                if (success) break;
                success = Emoji.TryParse($":{word}:", out emojiToReact);
                if (success) break;
            }

            if (emojiToReact != null)
            {
                await (await GetMessageToActOnForWakeWord(message)).AddReactionAsync(emojiToReact);
            }
        }

        if (CheckWordlist(message.Content, new List<string> { "bird", "burb", "birb" }))
        {
            if (random.Next(100) == 69)
            {
                await message.Channel.SendMessageAsync("bcdefghijklmnopqurstuvwxyz".ToCharArray()[random.Next(25)]
                    .ToString());
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

        if (message.Content.Contains("bird", StringComparison.CurrentCultureIgnoreCase) && message.Content.Contains("kill this", StringComparison.CurrentCultureIgnoreCase))
        {
            string deathMessage = mcDeaths[random.Next(mcDeaths.Length)];

            IMessage toActOn = await GetMessageToActOnForWakeWord(message);
            
            deathMessage = deathMessage.Replace("<player>", toActOn.Author.Mention);
            deathMessage = deathMessage.Replace("<entity>", toActOn.Author.Mention);
            deathMessage = deathMessage.Replace("<player/mob>", message.Author.Mention);
            deathMessage = deathMessage.Replace("<item>", "bird");
            
            await message.Channel.SendMessageAsync(deathMessage);
        }
    }

    [SlashCommand("bird", "bird")]
    public async Task BirdAsync()
    {
        var bird = random.Next(birds.Length);
        await RespondWithFileAsync(birds[bird]);
    }

    [SlashCommand("ytdl", "Download and send a youtube video")]

    public async Task YTDLAsync(string link)
    {
        await DeferAsync();
        var ytdl = new YoutubeDL();
        ytdl.YoutubeDLPath = Path.Combine(_config.GetValue<string>("ytdlpPath"), "yt-dlp");
        ytdl.FFmpegPath = Path.Combine(_config.GetValue<string>("ytdlpPath"), "ffmpeg");
        ytdl.OutputFolder = Program.GetYTDLPTempFolder;
        RunResult<string>? downloadResult = null;
        try
        {
            downloadResult = await ytdl.RunVideoDownload(url: link);
            await FollowupWithFileAsync(downloadResult.Data);
        }
        // catch (ArgumentException ex)
        // {
        //     await FollowupAsync("uhhhhh... link bad");
        //     throw ex;
        // }
        catch (Exception ex)
        {
            await FollowupAsync("That didnt work... Heres the error: \n " + ex.Message);
        }
        finally
        {
            if (downloadResult is not null)
            {
                File.Delete(downloadResult.Data);
            }
        }
        
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