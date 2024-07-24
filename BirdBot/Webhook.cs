using System.Net;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Utilities.FunctionCalling;

namespace Goatbot;

public class Webhook
{
    private readonly DiscordSocketClient _client;
    private IOpenAIService _openAiService; 
    private ITextChannel customerservice;
    private ITextChannel testingGrounds;
    private readonly IConfiguration _config;

    public Webhook(IConfiguration config, DiscordSocketClient client, IOpenAIService openAiService)
    {
        _config = config;
        _client = client;
        _openAiService = openAiService;
        client.Ready += ReadyAsync; 
    }

    private async Task ReadyAsync()
    {
        customerservice =
            (ITextChannel)_client.GetChannel(_config.GetSection("OpenAI").GetValue<ulong>("CustomerServiceChannelId"));
        testingGrounds = (ITextChannel) _client.GetChannel(_config.GetValue<ulong>("TestingGroundsId"));
    }

    public async Task Listen()
    {
        await WebhookListener(_config.GetValue<int>("WebhookPort"), _config.GetValue<string>("WebhookToken"));
    } 
    
    private async Task WebhookListener(int port, string token)
    {
        if (!HttpListener.IsSupported)
        {
            Console.WriteLine("The HttpListener namespace is not supported by your operating system or dotnet runtime. The webhook will be unavailable");
            return;
        }

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port.ToString()}/");
        listener.Start();
        Console.WriteLine("Webhook started");
        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            switch (context.Request.Url.AbsolutePath)
            {
                case "/":
                    if (context.Request.HttpMethod != "GET")
                    {
                        context.Response.StatusCode = 405;
                    }
                    else
                    {
                        context.Response.StatusCode = 204;
                    }
                    break;
                case "/customerservice":
                    if (context.Request.HttpMethod != "POST")
                    {
                        context.Response.StatusCode = 405;
                        break;
                    }

                    if (context.Request.Headers.Get("Authorization") == null)
                    {
                        context.Response.StatusCode = 401;
                        break;
                    }
                    if (context.Request.Headers.Get("Authorization") != $"Bearer {token}")
                    {
                        context.Response.StatusCode = 403;
                        break;
                    }
                    using (var body = context.Request.InputStream)
                    using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                    {
                        var data = JsonConvert.DeserializeObject<RequestModel>(await reader.ReadToEndAsync());
                        if (data == null)
                        {
                            context.Response.StatusCode = 400;
                            break;
                        }
                        var req = new ChatCompletionCreateRequest
                        {
                            Messages = new[]
                            {
                                ChatMessage.FromSystem(_config.GetSection("OpenAI")
                                    .GetValue<string>("CustomerServicePrompt")),
                                ChatMessage.FromSystem(data.Message)
                            },
                            Model = Models.Gpt_4o_mini,
                            Tools = FunctionCallingHelper.GetToolDefinitions<AITools>()
                        };
                        var reply = await _openAiService.ChatCompletion.CreateCompletion(req);
                        if (!reply.Successful)
                        {
                            throw new Exception(reply.Error?.Message);
                        }
                        if (data.TestMode)
                            await testingGrounds.SendMessageAsync(reply.Choices.First().Message.Content);
                        else 
                            await customerservice.SendMessageAsync(reply.Choices.First().Message.Content);
                    }
                    context.Response.StatusCode = 204;
                    break;
                default:
                    context.Response.StatusCode = 404;
                    break;
            }
            context.Response.Close();
        }
    }

    public class RequestModel
    {
        public string Message { get; set; }
        public bool TestMode { get; set; } = false;
    }
}