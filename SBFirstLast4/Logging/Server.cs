using System.Net.Http.Json;

namespace SBFirstLast4.Logging;

public static class Server
{
    private static readonly HttpClient Client = new();
    private const string ServerUrl = "https://sbfl4logging-lite.onrender.com/log";

    internal static async void Log<T>(T value)
    {
        if (!AppSettings.IsAdmin)
            await Client.PostAsJsonAsync(ServerUrl, value);
    }

    public static async Task<string> GetAsync()
    {
        try
        {
            var response = await Client.GetAsync(ServerUrl);

            response.EnsureSuccessStatusCode();

            var message = await response.Content.ReadAsStringAsync();

            return message;
        }
        catch (Exception ex)
        {
            return ex.Stringify();
        }
    }

    internal static async Task<bool> CheckAsync(string input)
    {
        try
        {
            var response = await Client.GetFromJsonAsync<AutoResponse>($"https://sbfl4logging-lite.onrender.com/auto?string={input}");
            return response?.Result ?? false;
        }
        catch
        {
            return false;
        }
    }

    /*
    internal static async Task<bool> ExistsAsync(string input, CancellationToken token = default)
    {
		try
		{
			var encodedInput = Uri.EscapeDataString(input);
			var response = await client.GetFromJsonAsync<TLResponse>($"https://sbfl4logging.onrender.com/typeless?string={encodedInput}", token);
			return response?.Exists ?? false;
		}
		catch
		{
			return true;
		}
	}
    */
}

file sealed class AutoResponse
{
    public bool Result { get; set; }
}

file sealed class TLResponse 
{
    public bool Exists { get; set; }
}
