#pragma warning disable IDE0230
using Blazored.LocalStorage;
using System.Text.Json;
using System.Security.Cryptography;
using SBFirstLast4.Logging;
using System.Data;

namespace SBFirstLast4;

internal static class AppSettings
{
	internal static string UserName { get; private set; } = "ILLEGAL_LOGIN";

	internal static string Guid { get; private set; } = "ILLEGAL_LOGIN";

	internal static string Hash { get; private set; } = "ILLEGAL_LOGIN";

	internal static bool IsLoggedIn { get; private set; } = false;

	internal static bool SortResult { get; private set; } = false;

	internal static bool UseExists { get; private set; } = false;

	internal static bool BetaMode { get; private set; } = false;

	internal static bool IsAdmin { get; private set; } = false;

	internal static string BattleBgm { get; set; } = "overflow";

	internal static async Task SetSortResult(ILocalStorageService localStorage, bool value) 
	{
		SortResult = value;
		await localStorage.SetItemAsync(LSKeys.SortResult, value);
	}
	internal static async Task SetUseExists(ILocalStorageService localStorage, bool value)
	{
		UseExists = value;
		await localStorage.SetItemAsync(LSKeys.UseExists, value);
	}

	internal static async Task SetBetaMode(ILocalStorageService localStorage, bool value)
	{
		BetaMode = value;
		await localStorage.SetItemAsync(LSKeys.BetaMode, value);
		Server.Log(new
		{
			Type = "TOGGLE_BETA",
			Value = value,
			UserInfo = new
			{
				Name = UserName,
				Guid,
				Hash
			},
			Date = DateTime.Now
		});
	}
	internal static async Task InitUserInfoAsync(ILocalStorageService localStorage, Func<string, Task> update)
	{
		IsLoggedIn = await localStorage.GetItemAsync<bool>(LSKeys.IsLoggedIn);
		if (!IsLoggedIn) return;

		UserName = await localStorage.GetItemAsync<string?>(LSKeys.UserName) ?? "ILLEGAL_LOGIN";
		Guid = await localStorage.GetItemAsync<string?>(LSKeys.UserId) ?? "ILLEGAL_LOGIN";
		Hash = await localStorage.GetItemAsync<string?>(LSKeys.HashId) ?? "ILLEGAL_LOGIN";

		await update("ユーザー設定を更新中...");
		await Task.WhenAny(SetIsAdminAsync(), Task.Delay(3000));
	}

	internal static async Task SetIsAdminAsync() => IsAdmin = await Server.CheckAsync(UserName);

	internal static async Task SetupAsync(ILocalStorageService localStorage) 
	{
		SortResult = await localStorage.GetItemAsync<bool>(LSKeys.SortResult);
		UseExists = await localStorage.GetItemAsync<bool>(LSKeys.UseExists);
		BetaMode = await localStorage.GetItemAsync<bool>(LSKeys.BetaMode);
	}

	internal static async Task SetUserInfoAsync(ILocalStorageService localStorage, string userName, string guid, string hash)
	{
		await localStorage.SetItemAsync(LSKeys.UserName, userName);
		await localStorage.SetItemAsync(LSKeys.UserId, guid);
		await localStorage.SetItemAsync(LSKeys.HashId, hash);
		IsLoggedIn = true;
		await localStorage.SetItemAsync(LSKeys.IsLoggedIn, IsLoggedIn);
		UserName = userName;
		Guid = guid;
		Hash = hash;
	}

	internal static async Task SetNameAsync(ILocalStorageService localStorage, string userName)
	{
		await localStorage.SetItemAsync(LSKeys.UserName, userName);
		UserName = userName;
	}

	internal static async Task RefreshHashAsync(HttpClient client, ILocalStorageService localStorage)
	{
		var hash = await GetHashAsync(client);
		await localStorage.SetItemAsync(LSKeys.HashId, hash);
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

		try
		{
			var response = await client.GetStringAsync(config);
			var json = JsonDocument.Parse(response);

			var hashRootStr = json.RootElement.GetProperty(key).GetString();

			if (hashRootStr is null) return "UNKNOWN_HASH";

			var hashRoot = Encoding.UTF8.GetBytes(hashRootStr);

			var hash = SHA256.HashData(hashRoot);

			if (hash is null) return "UNKNOWN_HASH";

			return hash.Select(b => b.ToString("x2")).StringJoin();
		}
		catch
		{
			return "UNKOWN_HASH";
		}
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

internal static class LSKeys
{
	internal const string
		UserName = "USER_NAME",
		UserId = "USER_ID",
		HashId = "HASH_ID",
		IsLoggedIn = "IS_LOGGED_IN",
		SortResult = "SORT_RESULT",
		UseExists = "USE_EXISTS",
		BetaMode = "BETA_MODE",
		HasLoaded = "hasLoaded",
		TypedWords = "typedWords",
		ModuleFiles = "MODULE_FILES";
}

file static class SerializationProvider
{
	internal static string Call(params byte[] bytes) => Encoding.UTF8.GetString(bytes);
}
