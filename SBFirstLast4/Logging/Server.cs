using Blazored.LocalStorage;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace SBFirstLast4.Logging;

public static class Server
{
	private static readonly HttpClient Client = new();
	private const string ServerUrl = "https://sbfl4logging-lite.onrender.com/log";

	internal static IJSRuntime? JSRuntime { get; set; }

	internal static Task Log(string type) => Log(type, new { });

	internal static Task Log<T>(string type, T order) => Log(StatusTemplate.Create(type, order));

	internal static async Task Log<T>(StatusTemplate<T> value)
	{
		if (!AppSettings.IsAdmin)
		{
			var response = await Client.PostAsJsonAsync(ServerUrl, value);
			response.EnsureSuccessStatusCode();
			return;
		}

#if NEVER
		if (!AppSettings.IsAdmin)
			return;

		var json = await JsonMirrorAsync(value);
		if (JSRuntime is not null)
			await JSRuntime.Alert(json);
#endif
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

	internal static async Task<bool> AuthorizeAsync(string input)
	{
		var debugAuth =
#if DEBUG
			true;
#else
			false;
#endif
		if (debugAuth)
		{
			try
			{
				var debugKey = await Client.GetStringAsync($"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/binary/.mys.txt");
				return debugKey.Trim() == input.Trim();
			}
			catch
			{
				return false;
			}
		}

		try
		{
			var response = await Client.GetFromJsonAsync<AutoResponse>($"https://sbfl4logging-lite.onrender.com/authorize?string={input}");
			return response?.Result ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static async Task<string> JsonMirrorAsync<T>(T json)
	{
		try
		{
			var response = await Client.PostAsJsonAsync("https://sbfl4logging-lite.onrender.com/jsonmirror", json);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}
		catch (Exception ex)
		{
			return ex.Stringify();
		}
	}

	internal static async Task<string?> SpeculateAsync(ILocalStorageService localStorage)
	{
		try
		{
			var guid = await localStorage.GetItemAsync<string?>(LSKeys.UserId) ?? string.Empty;
			var hash = await AppSettings.GetHashDirectlyAsync(Client);

			var response = await Client.PostAsync($"https://sbfl4logging-lite.onrender.com/speculate?string={hash}{guid}", null);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}
		catch
		{
			return null;
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