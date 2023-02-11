using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Modules;

public class Bird : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ulong[] voidIds;
    private readonly string[] birds;
    private static Random random = new Random();

    public Bird(DiscordSocketClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
        _client.MessageReceived += OnMessageAsync;
        voidIds = _config.GetSection("VoidIds").Get<ulong[]>();
        if (!string.IsNullOrEmpty(config.GetValue<string>("BirdsPath"))) {
            var birdsBasePath = config.GetValue<string>("BirdsPath");
            birds = Directory.GetFiles(birdsBasePath).Select(x => Path.Combine(birdsBasePath, x)).ToArray();
        }
    }

    public async Task OnMessageAsync(SocketMessage message)
    {
        if (voidIds.Contains(message.Channel.Id))
        {
            await message.DeleteAsync();
        }

        if (CheckWordlist(message.Content, new List<string> {"bird", "burb", "birb"}))
        {
            await message.Channel.SendMessageAsync("a");
        }

        if (message.Content.ToLower().Contains("fugl"))
        {
            await message.Channel.SendMessageAsync("Æ");
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
}