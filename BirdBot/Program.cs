using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    private IConfiguration _config;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private IServiceProvider _services;

    static void Main(string[] args)
        => new Program()
            .MainAsync()
            .GetAwaiter()
            .GetResult();


    public Program()
    {
        _client = new DiscordSocketClient(_socketConfig);
        _handler = new InteractionService(_client.Rest);

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _handler.SlashCommandExecuted += SlashCommandExecuted;
        _handler.Log += LogAsync;
    }

    public async Task MainAsync()
    {
        LoadConfig();
        _services = new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton(_socketConfig)
            .AddSingleton(_client)
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .BuildServiceProvider();
        await _client.LoginAsync(TokenType.Bot, _config.GetValue<string>("Token"));
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services); // Add modules
        _client.InteractionCreated += HandleInteraction; // add interaction handler
        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.All,
        AlwaysDownloadUsers = true,
        DefaultRetryMode = RetryMode.AlwaysFail
    };

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        break;
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"{_client.CurrentUser} is connected!");
        await RegisterCommands();
        await SetStatus();
    }

    private void LoadConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        _config = builder.Build();
    }

    private async Task SetStatus()
    {
        if (!string.IsNullOrEmpty(_config.GetValue<string>("Status:Content"))) await _client.SetGameAsync(_config.GetValue<string>("Status:Content"), type: Enum.Parse<ActivityType>(_config.GetValue<string>("Status:Type")));
    }

    private async Task RegisterCommands()
    {
        if (_config.GetValue<bool>("AutoRegisterSlashCommands:Prod"))
        {
            await _handler.RegisterCommandsGloballyAsync(true);
        }

        if (_config.GetValue<bool>("AutoRegisterSlashCommands:Dev") && (_config.GetSection("DevServers").Get<ulong[]?>()?.Any() ?? false))
        {
            _config.GetSection("DevServers").Get<ulong[]>().ToList().ForEach(async guild => await _handler.RegisterCommandsToGuildAsync(guild, true));
        }
    }

    private async Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
        if (!arg3.IsSuccess)
        {
            switch (arg3.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    break;
                case InteractionCommandError.UnknownCommand:
                    break;
                case InteractionCommandError.BadArgs:
                    break;
                case InteractionCommandError.Exception:
                    Console.WriteLine($"Command exception: {arg3.ErrorReason}");
                    break;
                case InteractionCommandError.Unsuccessful:
                    Console.WriteLine($"Command could not be executed");
                    break;
                default:
                    Console.WriteLine($"An unknown error occured");
                    break;
            }
        }
    }
}