// using System.Diagnostics;
// using Discord;
// using Discord.Audio;
// using Discord.Interactions;
// using Discord.WebSocket;
//
// namespace Goatbot.Modules;
//
// public class Elevator : InteractionModuleBase<SocketInteractionContext>
// {
//     private IVoiceChannel elevator;
//     private readonly DiscordSocketClient _client;
//
//     public Elevator(DiscordSocketClient client)
//     {
//         _client = client;
//         client.Ready += ReadyAsync;
//     }
//
//     public async Task ReadyAsync()
//     {
//         elevator = await _client.GetChannelAsync(1200631028809748520) as IVoiceChannel;
//         Task.Run(GOOOOOOOOOOOOOOOOOOOOOOOOOOO);
//     }
//
//     private async Task GOOOOOOOOOOOOOOOOOOOOOOOOOOO()
//     {
//         var audioClient = await elevator.ConnectAsync();
//         await PlayAudioAsync(audioClient);
//     }
//     
//     private async Task PlayAudioAsync(IAudioClient client)
//     {
//         while (true) // Loop indefinitely
//         {
//             using (var ffmpeg = CreateProcess(Path.Combine(Directory.GetCurrentDirectory(), "elevator.mp3")))
//             using (var stream = client.CreatePCMStream(AudioApplication.Music))
//             {
//                 try
//                 {
//                     await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
//                 }
//                 finally
//                 {
//                     await stream.FlushAsync();
//                 }
//             }
//         }
//     }
//
//     private Process CreateProcess(string path)
//     {
//         return Process.Start(new ProcessStartInfo
//         {
//             FileName = "ffmpeg",
//             Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
//             UseShellExecute = false,
//             RedirectStandardOutput = true,
//             CreateNoWindow = true
//         });
//     }
// }