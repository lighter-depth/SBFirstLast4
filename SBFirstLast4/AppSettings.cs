#pragma warning disable IDE0230
using Blazored.LocalStorage;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using SBFirstLast4.Logging;

namespace SBFirstLast4;

internal static class AppSettings
{
	internal static string UserName { get; private set; } = "ILLEGAL_LOGIN";

	internal static string Guid { get; private set; } = "ILLEGAL_LOGIN";

	internal static string Hash { get; private set; } = "ILLEGAL_LOGIN";

	internal static bool IsLoggedIn { get; private set; } = false;

	internal static bool SortResult { get; private set; } = false;

	internal static bool IsAdmin { get; private set; } = false;

	internal static async Task SetSortResult(ILocalStorageService localStorage, bool value) 
	{
		SortResult = value;
		await localStorage.SetItemAsync("SORT_RESULT", value);
	}

	internal static async Task InitUserInfoAsync(ILocalStorageService localStorage)
	{
		IsLoggedIn = await localStorage.GetItemAsync<bool>("IS_LOGGED_IN");
		if (!IsLoggedIn) return;

		UserName = await localStorage.GetItemAsync<string>("USER_NAME");
		Guid = await localStorage.GetItemAsync<string>("USER_ID");
		Hash = await localStorage.GetItemAsync<string>("HASH_ID");

		await SetIsAdminAsync();
	}

	internal static async Task SetIsAdminAsync() => IsAdmin = await Server.CheckAsync(UserName);

	internal static async Task SetupAsync(ILocalStorageService localStorage) 
	{
		SortResult = await localStorage.GetItemAsync<bool>("SORT_RESULT");
	}

	internal static async Task SetUserInfoAsync(ILocalStorageService localStorage, string userName, string guid, string hash)
	{
		await localStorage.SetItemAsync("USER_NAME", userName);
		await localStorage.SetItemAsync("USER_ID", guid);
		await localStorage.SetItemAsync("HASH_ID", hash);
		IsLoggedIn = true;
		await localStorage.SetItemAsync("IS_LOGGED_IN", IsLoggedIn);
		UserName = userName;
		Guid = guid;
		Hash = hash;
	}

	internal static async Task SetNameAsync(ILocalStorageService localStorage, string userName)
	{
		await localStorage.SetItemAsync("USER_NAME", userName);
		UserName = userName;
	}

	internal static async Task RefreshHashAsync(HttpClient client, ILocalStorageService localStorage)
	{
		var hash = await GetHashAsync(client);
		await localStorage.SetItemAsync("HASH_ID", hash);
		Hash = hash;
	}


	public static async Task<string> GetHashAsync(HttpClient client)
	{
		var config = SerializationProvider.Call(
			0b01101000,
			0b01110100,
			0b01110100,
			0b01110000,
			0b01110011,
			0b00111010,
			0b00101111,
			0b00101111,
			0b01100001,
			0b01110000,
			0b01101001,
			0b00101110,
			0b01101001,
			0b01110000,
			0b01101001,
			0b01100110,
			0b01111001,
			0b00101110,
			0b01101111,
			0b01110010,
			0b01100111,
			0b00101111,
			0b00111111,
			0b01100110,
			0b01101111,
			0b01110010,
			0b01101101,
			0b01100001,
			0b01110100,
			0b00111101,
			0b01101010,
			0b01110011,
			0b01101111,
			0b01101110
			);

		var key = SerializationProvider.Call(
			0b01101001,
			0b01110000
			);

		var response = await client.GetStringAsync(config);
		var json = JsonDocument.Parse(response);

		var hashRootStr = json.RootElement.GetProperty(key).GetString();

		if (hashRootStr is null) return "UNKNOWN_HASH";

		var hashRoot = Encoding.UTF8.GetBytes(hashRootStr);

		var hash = SHA256.HashData(hashRoot);

		if (hash is null) return "UNKNOWN_HASH";

		return string.Join(string.Empty, hash.Select(b => b.ToString("x2")));
	}


	internal static bool SkipFlag { get; private set; } =
#if DEBUG
	false
#else
	false
#endif
;
	internal static bool IsDebug { get; private set; } =
#if DEBUG
	true
#else
	false
#endif
	;
}

file static class SerializationProvider
{
	internal static string Call(params byte[] bytes) => Encoding.UTF8.GetString(bytes);
}
