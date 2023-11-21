using System.Diagnostics;
using System.Net.Http.Json;

namespace SBFirstLast4.Logging;

public static class Server
{
	private static readonly HttpClient client = new();
	private const string SERVER_URL = "https://sbfl4logging.onrender.com/log";

	internal static async void Post<T>(T value)
	{
#if DEBUG
		await Task.CompletedTask;
#else
		await client.PostAsJsonAsync(SERVER_URL, value);
#endif
	}

	public static async Task<string> GetAsync()
	{
		try
		{
			var response = await client.GetAsync(SERVER_URL);

			response.EnsureSuccessStatusCode();

			var message = await response.Content.ReadAsStringAsync();

			return message;
		}
		catch (Exception ex)
		{
			return $"{ex.GetType().Name} {ex.Message}";
		}
	}
}


