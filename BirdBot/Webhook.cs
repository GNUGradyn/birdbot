using System.Net;

namespace Goatbot;

public class Webhook
{
    public static async Task WebhookListener(int port)
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
                        context.Response.OutputStream.Close();
                        break;
                    }
                    context.Response.StatusCode = 204;
                    context.Response.OutputStream.Close();
                    break;
                default:
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                    break;
            }
        }
    }
}