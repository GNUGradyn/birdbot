using System.Collections;
using Discord;
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
    private ITextChannel customerservice;
    private readonly IConfiguration _config;
    public OpenAI(IOpenAIService openAiService, IConfiguration config, DiscordSocketClient client)
    {
        _openAiService = openAiService;
        _config = config;
        _client = client;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageAsync;
    }

    private static bool _readyLock;
    
    private async Task ReadyAsync()
    {
        if (_readyLock) return;
        _readyLock = true;
        general = (ITextChannel) _client.GetChannel(_config.GetSection("OpenAI").GetValue<ulong>("GeneralChannelId"));
        customerservice = (ITextChannel) _client.GetChannel(_config.GetSection("OpenAI").GetValue<ulong>("CustomerServiceChannelId"));
        Task.Run(StartTimer);
    }

    public async Task MessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        if (message.Channel.Id == customerservice.Id)
        {
            using (message.Channel.EnterTypingState())
            {
                var messages = new MessageReplyChainLinkedList();
                messages.AddNode(new MessageReplyChainLinkedList.Node(ChatMessage.FromUser(message.Content)));
                ulong? head = message.Reference?.MessageId.Value;
                while (head != null)
                {
                    var discordMessage = await customerservice.GetMessageAsync(head.Value);
                    if (discordMessage.Author.Id == _client.CurrentUser.Id)
                        messages.AddNode(new MessageReplyChainLinkedList.Node(ChatMessage.FromAssistant(discordMessage.Content)));
                    else
                    {
                        var content = discordMessage.Content;
                        if (message.Attachments.Count > 0) content += "\n[Image]";
                        messages.AddNode(new MessageReplyChainLinkedList.Node(ChatMessage.FromUser(content)));
                    }

                    head = message.Reference?.MessageId.Value;
                    if (discordMessage.Id == head) head = null; // Workaround for dumb discord.net bug im too lazy to fix properly
                }
                messages.AddNode(new MessageReplyChainLinkedList.Node(ChatMessage.FromSystem(_config.GetSection("OpenAI").GetValue<string>("CustomerServicePrompt"))));
                messages.Reverse();
                var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = messages.ToList(),
                    Model = Models.Gpt_4_turbo_preview,
                });
                await customerservice.SendMessageAsync(completionResult.Choices.First().Message.Content, messageReference: new MessageReference(message.Id));
            }
        }
    }

    public async Task StartTimer()
    {
        while (true)
        {
            await Task.Delay(rand.Next((int)TimeSpan.FromDays(1).TotalMilliseconds, (int)TimeSpan.FromDays(7).TotalMilliseconds));
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
                else
                {
                    result = $"{x.Author.Username}: {x.Content}";

                }
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
    
    private class MessageReplyChainLinkedList : IEnumerable<ChatMessage>
    {
        public Node head;

        public class Node
        {
            public ChatMessage data;
            public Node next;

            public Node(ChatMessage message)
            {
                data = message;
                next = null;
            }
        }
    
        public void AddNode(Node node)
        {
            if (head == null) head = node;
            else
            {
                Node temp = head;
                while (temp.next != null) temp = temp.next;
                temp.next = node;
            }
        }

        public void Reverse()
        {
            Node prev = null;
            Node current = head;
            Node next = null;

            while (current != null)
            {
                next = current.next;
                current.next = prev;
                prev = current;
                current = next;
            }
            head = prev;
        }

        public IEnumerator<ChatMessage> GetEnumerator()
        {
            Node current = head;
            while (current != null)
            {
                yield return current.data;
                current = current.next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}