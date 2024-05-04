﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Goatbot.Modules;

public class OpenAI : InteractionModuleBase<SocketInteractionContext>
{
    private IOpenAIService _openAiService; 
    private readonly DiscordSocketClient _client;
    private static readonly Random rand = new();
    private ITextChannel general;
    private readonly IConfiguration _config;
    public OpenAI(IOpenAIService openAiService, IConfiguration config, DiscordSocketClient client)
    {
        _openAiService = openAiService;
        _config = config;
        _client = client;
        _client.Ready += ReadyAsync;
    }

    private static bool _readyLock;
    
    private async Task ReadyAsync()
    {
        if (_readyLock) return;
        _readyLock = true;
        general = (ITextChannel) _client.GetChannel(_config.GetSection("OpenAI").GetValue<ulong>("GeneralChannelId"));
        Task.Run(StartTimer);
    }

    public async void StartTimer()
    {
        while (true)
        {
            Thread.Sleep(rand.Next((int)TimeSpan.FromDays(1).TotalMinutes, (int)TimeSpan.FromDays(7).TotalMinutes));
            var messagesForContext = (await general
                .GetMessagesAsync(_config.GetSection("OpenAI").GetValue<int>("ContextSize")).FlattenAsync()).Reverse();
            var context = await Task.WhenAll(messagesForContext.Select(async x =>
            {
                var result = "";
                if (x.Reference != null && x.Reference.MessageId.IsSpecified)
                {
                    result =
                        $"{x.Author.Username} (in response to \"{(await general.GetMessageAsync(x.Reference.MessageId.Value)).Content})\": {x.Content}";
                }

                result = $"{x.Author.Username}: {x.Content}";
                if (x.Attachments.Count > 0) result += "[Image]";
                return result;
            }));
            string prompt = _config.GetSection("OpenAI").GetValue<string>("RandomButtInPrompt") + "\n\n" +
                            string.Join(Environment.NewLine, context);

            var completionResult =
                await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(prompt)
                    },
                    Model = Models.Gpt_4
                });

        await general.SendMessageAsync(completionResult.Choices.First().Message.Content);
        }
    }
}