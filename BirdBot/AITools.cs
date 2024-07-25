using OpenAI.Utilities.FunctionCalling;

namespace Goatbot;

public class AITools
{
    public static string ship24bearer; // TODO: figure out a way to expose this with dependency injection instead of this
    
    [FunctionDescription(Name = "porch", Description = "Attaches a live snapshot of the porch from a camera to the current message")]
    public async Task<byte[]> PorchCamera()
    {
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync("http://192.168.0.46/snap.jpeg");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}