using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace Goatbot.Modules
{
    internal class Ravioli
    {
        private static DiscordSocketClient _client;

        private static readonly Random Rand = new();
        private static readonly string[] Text = {
            "ravioli",
            "RaViOlI",
            "ᴉloᴉʌɐɹ",
            "ɹɐʌᴉolᴉ",
            "iloivar",
            "✊💎  尺𝓐V𝓲𝔬ᒪ𝕚  ♗♧",
            "𝖗𝖆𝖛𝖎𝖔𝖑𝖎",
            "𝔯𝔞𝔳𝔦𝔬𝔩𝔦",
            "r⃣   a⃣   v⃣   i⃣   o⃣   l⃣   i⃣",
            "🅁🄰🅅🄸🄾🄻🄸",
            "r̶͖̦̥̺̊̌̾̀́̾̓̓̀̕a̷̝̮͚̞̩͛̚v̵̡͑̓͋͛̾̍̎̏̚͠i̶̙̓̄̀ȍ̸͈͖͊͊̀̋͘͝ĺ̶͙͙͉͂͊͂̿̌͒̋͘̚i̷̭̎",
            "ᏒᏗᏉᎥᎧᏝᎥ",
            "ཞą۷ıơƖı",
            "ЯΛVIӨᄂI",
            "尺卂ᐯ丨ㄖㄥ丨",
            "(っ◔◡◔)っ ♥ ravioli ♥"
        };

        public static void RavioliRavioliWhatsInThePocketoli(DiscordSocketClient client)
        {
            _client = client;
            Task.Run(StartTimer);
        }
        private static async Task StartTimer()
        {
            var minMinutes = (int)TimeSpan.FromHours(12).TotalMinutes;
            var maxMinutes = (int)TimeSpan.FromDays(30).TotalMinutes;

            var randMinutes = Rand.Next(minMinutes, maxMinutes);
            var time = TimeSpan.FromMinutes(randMinutes);

            await Task.Delay(time);

            var message  = Text[Rand.Next(Text.Length - 1)];
            var guild = _client.GetGuild(595687467827462144);
            var ch = guild.GetTextChannel(642613389583056897);

            await ch.SendMessageAsync(message);

            Task.Run(StartTimer);
        }
    }
}
