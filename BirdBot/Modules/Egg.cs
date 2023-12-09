using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Goatbot.Modules;

public class Egg : InteractionModuleBase<SocketInteractionContext>
{
    private readonly string[] eggs = new[]
    {
        "<:egg1:1181038948940796064>", 
        "<:egg2:1181039008076267631>", 
        "<:egg3:1181039614706864240>", 
        "<:egg4:1181039646235443210>", 
        "<:egg5:1181039681748611123>", 
        "<:egg6:1181039752682684527>", 
        "<:egg7:1181039800661323836>",
        "<:egg8:1181039828146606121>",
        "<:egg9:1181039854566514820>",
        "<:egg10:1181039881502343329>",
        "<:egg11:1181039911411920936>",
        "<:egg12:1181039942089064548>",
        "<:egg13:1181039980794085476>",
        "<:egg14:1181040002541572158>",
        "<:egg15:1181040028282015805>",
        "<:egg16:1181040059563122698>",
        "<:egg17:1181040085098057871>",
        "<:egg18:1181040113451540572>",
        "<:egg19:1181040146347466763>",
        "<:egg20:1181040179440537684>"
    };
    
    private readonly DiscordSocketClient _client;

    public Egg(DiscordSocketClient client)
    {
        _client = client;
        client.MessageCommandExecuted += MessageCommandHandler;
        client.Ready += ReadyAsync;
        client.UserCommandExecuted += UserCommandHandler;
    }

    public async Task ReadyAsync()
    {
        var egg = new MessageCommandBuilder();
        egg.WithName("egg");

        var impersonate = new UserCommandBuilder();
        impersonate.WithName("Impersonate");

        try
        {
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(new ApplicationCommandProperties[]
            {
                egg.Build(),
                impersonate.Build()
            });
        }    
        catch(ApplicationCommandException exception)
        {
            var json = JsonConvert.SerializeObject(exception, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    
    public async Task MessageCommandHandler(SocketMessageCommand cmd)
    {
        if (cmd.CommandName == "egg")
        {
            await cmd.RespondAsync("Aight please hold on the line while i lay some eggs", ephemeral: true);
            await Task.WhenAll(eggs.Select(x => cmd.Data.Message.AddReactionAsync(Emote.Parse(x))));
            await cmd.ModifyOriginalResponseAsync(properties => properties.Content = "That should do it boss");
        }
    }
    
    public async Task UserCommandHandler(SocketUserCommand cmd)
    {
        if (cmd.CommandName == "impersonate")
        {
            var modal = new ModalBuilder().WithTitle("Impersonation menu")
                .AddTextInput("What should I say?", "body", TextInputStyle.Paragraph, "my mom gae")
                .AddTextInput("Where da fuk (channel ID, leave empty for general)", "channel", TextInputStyle.Paragraph, "1234567890");
            await RespondWithModalAsync(modal.Build());
        }
    }
}