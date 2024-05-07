using OpenAI.Utilities.FunctionCalling;

namespace Goatbot;

public class AITools
{
    public static string ship24bearer; // TODO: figure out a way to expose this with dependency injection instead of this
    
    [FunctionDescription("Provides a tracking history for the switch")]
    public string TrackTheSwitch()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.ship24.com/public/v1/trackers/fbe2cf4e-4254-438c-b42c-14e01bdcfe1f/results");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Authorization", $"Bearer {ship24bearer}");
        var response = client.Send(request);
        response.EnsureSuccessStatusCode();
        return new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
    }
}