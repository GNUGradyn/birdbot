using OpenAI.Utilities.FunctionCalling;

namespace Goatbot;

public class AITools
{
    [FunctionDescription("Provides a tracking history for the switch")]
    public string TrackTheSwitch()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.ship24.com/public/v1/trackers/fbe2cf4e-4254-438c-b42c-14e01bdcfe1f/results");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Authorization", "Bearer apik_hcgygcGueC8vDJM5jJcAvXfY3zo2NT");
        var response = client.Send(request);
        response.EnsureSuccessStatusCode();
        return new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
    }
}