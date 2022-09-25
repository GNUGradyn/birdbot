﻿using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Modules;

public class Bird : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ulong voidId;
    private readonly string[] birds;
    private static Random random = new Random();

    public Bird(DiscordSocketClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
        _client.MessageReceived += OnMessageAsync;
        voidId = _config.GetValue<ulong>("VoidId");
        var birdsBasePath = config.GetValue<string>("BirdsPath");
        birds = Directory.GetFiles(birdsBasePath).Select(x => Path.Combine(birdsBasePath, x)).ToArray();
    }

    public async Task OnMessageAsync(SocketMessage message)
    {
        if (message.Channel.Id == voidId)
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

        if (message.Author.Id == 557643831156408371)
        {
            await message.Channel.SendMessageAsync("a");
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