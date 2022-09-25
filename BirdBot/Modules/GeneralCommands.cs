using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Goatbot.Modules;

public class GeneralCommands : InteractionModuleBase<SocketInteractionContext>
{
    static Random rnd = new Random();

    public static T Choice<T>(IEnumerable<T> options)
    {
        return options.ElementAt(rnd.Next(options.Count()));
    }

    public enum RpsOptions
    {
        Rock,
        Paper,
        Scissors
    }

    static string[] options = { "Yes", "No", "Most Likely", "Certianly not", "Absolutely", "My reply is no", "Signs point to yes", "Don't count on it", "Outlook not so good", "Definitely" };

    [SlashCommand("ping", "Pings the bot and returns its latency.")]
    public async Task PingAsync()
        => await RespondAsync(text: $":ping_pong: Pong! {Context.Client.Latency}ms");

    [SlashCommand("8ball", "Ask the magic 8 ball anything and the truth will be revealed")]
    public async Task Magic8BallAsync()
        => await RespondAsync(text: $"🎱 {Choice(options)}");

    [SlashCommand("choose", "Choose from a list of things")]
    public async Task ChooseAsync(string choices)
        => await RespondAsync(text: Choice(choices.Split(" ")));

    [SlashCommand("flip", "Flip a coin")]
    public async Task FlipAsync()
        => await RespondAsync(text: Choice(new string[] { "Heads", "Tales" }));

    [SlashCommand("roll", "Chooses a random number 1-6 or 1 thru a specified number")]
    public async Task RollAsync(int? max = null)
        => await RespondAsync(text: rnd.Next(1, max ?? 6).ToString());

    [SlashCommand("rps", "A classic rock paper scissors game")]
    public async Task RpsAsync(RpsOptions option)
    {
        var computer = (RpsOptions)rnd.Next(Enum.GetNames(typeof(RpsOptions)).Length);
        if (option == computer) await RespondAsync(text: $"I choose {option}, it's a draw!");
        if (computer == RpsOptions.Paper && option == RpsOptions.Rock) await RespondAsync(text: "I Choose Paper, you lose!");
        if (computer == RpsOptions.Scissors && option == RpsOptions.Rock) await RespondAsync("I Choose Scissors, you win!");
        if (computer == RpsOptions.Rock && option == RpsOptions.Paper) await RespondAsync("I Choose Rock, you win!");
        if (computer == RpsOptions.Scissors && option == RpsOptions.Paper) await RespondAsync("I Choose Scissors, you lose!");
        if (computer == RpsOptions.Paper && option == RpsOptions.Scissors) await RespondAsync("I Choose Paper, you Win!");
        if (computer == RpsOptions.Rock && option == RpsOptions.Scissors) await RespondAsync("I Choose Rock, you lose!");
    }

    [SlashCommand("serverinfo", "Get info about the current server")]
    public async Task ServerInfoAsync()
    {
        EmbedBuilder builder = new EmbedBuilder();
        builder.WithTitle($"Information for {Context.Guild.Name}");
        builder.AddField("Owner", Context.Guild.Owner.Mention, true);
        builder.AddField("Id", Context.Guild.Id, true);
        builder.AddField("Members", Context.Guild.MemberCount, true);
        builder.AddField("Created", Context.Guild.CreatedAt, true);
        builder.ThumbnailUrl = Context.Guild.IconUrl;
        await RespondAsync(embed: builder.Build());
    }

    [SlashCommand("userinfo", "Gets info about you or another user")]
    public async Task UserInfoAsync(ulong? userId = null, string? username = null, IUser? ping = null)
    {
        IUser user = Context.User;

        if (ping != null) user = ping;
        else if (username != null) user = (await Context.Guild.GetUsersAsync().FlattenAsync()).First(x => x.Username == username || x.Nickname == username);
        else if (userId != null) user = Context.Guild.GetUser(userId.Value);

        if (user == null)
        {
            await RespondAsync("User not found");
        }
        else
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"Information for {user.Username}#{user.Discriminator}");
            builder.ThumbnailUrl = user.GetAvatarUrl();
            builder.AddField("Id", user.Id, true);
            builder.AddField("Created at", user.CreatedAt, true);
            if (user.ActiveClients.Any()) builder.AddField("Active on", $"{string.Join(", ", user.ActiveClients.Select(x => x.ToString()))}", true);
            if ((user as SocketGuildUser).Nickname != null) builder.AddField("Nickname", (user as SocketGuildUser).Nickname, true); // TODO: make this work in DMs maybe
            builder.AddField("Joined at", (user as SocketGuildUser).JoinedAt, true);
            builder.AddField("Status", user.Status, true);
            if (user.Activities.Any())
            {
                List<String> statuses = new List<string>();
                foreach (var activity in user.Activities)
                {
                    // TODO: make this a switch and support more types
                    if (activity.Type == ActivityType.Playing) statuses.Add($"Playing {activity.Name}");
                    if (activity.Type == ActivityType.CustomStatus) statuses.Add($"{((CustomStatusGame)activity).Emote} {((CustomStatusGame)activity).State}");
                    if (activity.Type == ActivityType.Listening) statuses.Add($"Listening to {activity.Name}");
                }

                builder.AddField("Statuses", string.Join(", ", statuses));
            }

            await RespondAsync(embed: builder.Build());
        }
    }
}