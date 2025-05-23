﻿using Blazored.LocalStorage;
using System.Text.Json;
using System.Security.Cryptography;
using SBFirstLast4.Logging;
using System.Data;
using SBFirstLast4.Shared;
using Microsoft.JSInterop;
using static System.Net.WebRequestMethods;

namespace SBFirstLast4;

internal static class AppSettings
{
	internal static string UserName { get; private set; } = IllegalLogin;

	internal static string Guid { get; private set; } = IllegalLogin;

	internal static string Hash { get; private set; } = IllegalLogin;

	internal static string V4 { get; private set; } = IllegalLogin;

	internal static string V6 { get; private set; } = IllegalLogin;

	internal static bool IsLoggedIn { get; private set; } = false;

	internal static bool SortResult { get; private set; } = false;

	internal static bool UseExists { get; private set; } = false;

	internal static bool BetaMode { get; private set; } = false;

	internal static bool VolatileMode
	{
#if VOLATILE_MODE
		get	=> _volatileMode;
#else
		get	=> false;
#endif
		private set => _volatileMode = value;
	}

	private static bool _volatileMode;

	internal static bool IsAdmin { get; private set; } = false;

	internal static string BattleBgm { get; set; } = "overflow";

	internal static bool IsOffline { get; set; } = true;

	internal static bool IsUnderMaintainance { get; set; } = false;

	internal static bool IsBlocking { get; set; } = false;

	internal static bool IsObsolete { get; set; } = false;

	internal static bool IsAccepted { get; set; } = false;

	private static string CachedAuthKey { get; set; } = string.Empty;

	private const string IllegalLogin = "ILLEGAL_LOGIN";

	internal static bool IsIllegalLogin => Hash is IllegalLogin || V4 is IllegalLogin || V6 is IllegalLogin;

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
		await Server.Log
		(
			type: StatusTypes.ToggleBeta,
			order: new
			{
				Value = value
			}
		);
	}

	internal static async Task SetVolatileMode(ILocalStorageService localStorage, bool value)
	{
		VolatileMode = value;
		await localStorage.SetItemAsync(LSKeys.VolatileMode, value);
		if (NavMenu.Instance is { } instance)
			instance.RaiseStateHasChanged();
	}

	internal static async Task InitOnlineStatusAsync(HttpClient client, ILocalStorageService localStorage)
	{
		CachedAuthKey = await localStorage.GetItemAsync<string?>(LSKeys.CachedAuthKey) ?? string.Empty;
		string? status;
		try
		{
			status = await client.GetStringAsync($"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/binary/.dsync.txt");
		}
		catch (Exception ex)
		when (ex is HttpRequestException hex)
		{
			var statusCode = hex.StatusCode;
			var msg = hex.Message;

			var sss = $"{statusCode}: {msg}";

			IsOffline = true;
			return;
		}
		IsOffline = false;
		var items = status.Split(';').Select(x => x.Trim()).ToArray();
		IsUnderMaintainance = bool.Parse(items[0]);
		IsBlocking = await IsBlockingAsync(items[1]);
		var requiredVersion = new Version(items[2]);
		var requiredGeneration = int.Parse(items[3]);

		if (IsUnderMaintainance || IsBlocking)
			return;

		var currentVersion = Versioning.VersionHistory.Latest;
		var currentGeneration = Versioning.Generation.Current;
		if (requiredVersion > currentVersion || requiredGeneration > currentGeneration)
		{
			IsObsolete = true;
			return;
		}

		static async Task<bool> IsBlockingAsync(string str)
		{
			var isBlocking = bool.Parse(str);
			if (!isBlocking)
				return false;

			if (string.IsNullOrWhiteSpace(CachedAuthKey))
				return true;

			var isAcceptable = await Server.AuthorizeAsync(CachedAuthKey);
			if (!isAcceptable)
				return true;

			return false;
		}
	}

	internal static async Task InitUserInfoAsync(ILocalStorageService localStorage, Func<string, Task> update)
	{
		IsLoggedIn = await localStorage.GetItemAsync<bool>(LSKeys.IsLoggedIn);
		if (!IsLoggedIn) return;

		UserName = await localStorage.GetItemAsync<string?>(LSKeys.UserName) ?? IllegalLogin;
		Guid = await localStorage.GetItemAsync<string?>(LSKeys.UserId) ?? IllegalLogin;
		Hash = await localStorage.GetItemAsync<string?>(LSKeys.HashId) ?? IllegalLogin;
		V4 = await localStorage.GetItemAsync<string?>(LSKeys.HashV4) ?? IllegalLogin;
		V6 = await localStorage.GetItemAsync<string?>(LSKeys.HashV6) ?? IllegalLogin;

		await update("ユーザー設定を更新中...");
		await Task.WhenAny(SetIsAdminAsync(), Task.Delay(3000));
	}

	internal static async Task SetIsAdminAsync() => IsAdmin = await Server.CheckAsync(UserName);

	internal static async Task SetupAsync(ILocalStorageService localStorage)
	{
		SortResult = await localStorage.GetItemAsync<bool>(LSKeys.SortResult);
		UseExists = await localStorage.GetItemAsync<bool>(LSKeys.UseExists);
		BetaMode = await localStorage.GetItemAsync<bool>(LSKeys.BetaMode);
		VolatileMode = await localStorage.GetItemAsync<bool>(LSKeys.VolatileMode);
	}

	internal static async Task SetUserInfoAsync(ILocalStorageService localStorage, string userName, string guid, string hash, string v4, string v6)
	{
		await localStorage.SetItemAsync(LSKeys.UserName, userName);
		await localStorage.SetItemAsync(LSKeys.UserId, guid);
		await localStorage.SetItemAsync(LSKeys.HashId, hash);
		await localStorage.SetItemAsync(LSKeys.HashV4, v4);
		await localStorage.SetItemAsync(LSKeys.HashV6, v6);
		IsLoggedIn = true;
		await localStorage.SetItemAsync(LSKeys.IsLoggedIn, IsLoggedIn);
		UserName = userName;
		Guid = guid;
		Hash = hash;
		V4 = v4;
		V6 = v6;
	}

	internal static async Task SetNameAsync(ILocalStorageService localStorage, string userName)
	{
		await localStorage.SetItemAsync(LSKeys.UserName, userName);
		UserName = userName;
	}

	internal static async Task RefreshHashAsync(HttpClient client, ILocalStorageService localStorage)
	{
		var (hash, v4, v6) = await GetHashAsync(client);
		await localStorage.SetItemAsync(LSKeys.HashId, hash);
		await localStorage.SetItemAsync(LSKeys.HashV4, v4);
		await localStorage.SetItemAsync(LSKeys.HashV6, v6);
		Hash = hash;
		V4 = v4;
		V6 = v6;
	}


	public static async Task<(string Hash, string V4, string V6)> GetHashAsync(HttpClient client)
	{
		const string UnknownHash = "UNKNOWN_HASH";
		try
		{
			var responseV4 = await client.GetStringAsync("https://api.ipify.org/?format=json");
			var responseV6 = await client.GetStringAsync("https://api64.ipify.org/?format=json");
			var jsonV4 = JsonDocument.Parse(responseV4);
			var jsonV6 = JsonDocument.Parse(responseV6);

			var hashRootStr = jsonV4.RootElement.GetProperty("ip").GetString();
			var v6Str = jsonV6.RootElement.GetProperty("ip").GetString() ?? UnknownHash;

			if (hashRootStr is null)
				return (UnknownHash, UnknownHash, v6Str);

			var hashRoot = Encoding.UTF8.GetBytes(hashRootStr);

			var hash = SHA256.HashData(hashRoot);

			if (hash is null) return (UnknownHash, hashRootStr, v6Str);

			return (hash.Select(b => b.ToString("x2")).StringJoin(), hashRootStr, v6Str);
		}
		catch
		{
			return (UnknownHash, UnknownHash, UnknownHash);
		}
	}
	internal static async Task<string> TemporaryHashAsync(HttpClient client)
	{
		var guid = System.Guid.NewGuid().ToString();
		var hash = await GetHashDirectlyAsync(client);
		var guidTrim = guid.Take(6).StringJoin();
		var hashTrim = hash.Take(6).StringJoin();
		return $"g1{guidTrim}h1{hashTrim}";
	}

	internal static async Task<string> GetHashDirectlyAsync(HttpClient client)
	{
		const string UnknownHash = "UNKNOWN_HASH";
		var responseV4 = await client.GetStringAsync("https://api.ipify.org/?format=json");
		var jsonV4 = JsonDocument.Parse(responseV4);
		var hashRootStr = jsonV4.RootElement.GetProperty("ip").GetString();
		if (hashRootStr is null)
			return UnknownHash;
		var hashRoot = Encoding.UTF8.GetBytes(hashRootStr);
		var hash = SHA256.HashData(hashRoot);
		return hash.Select(b => b.ToString("x2")).StringJoin();
	}

	internal static async Task<bool> ShouldReturn(HttpClient client)
	{
		var result = await ShouldReturnCore(client);
		if (result)
			IsAccepted = false;
		return result;

		static async Task<bool> ShouldReturnCore(HttpClient client)
		{
			if (IsIllegalLogin)
				return true;

			string? status;
			try
			{
				status = await client.GetStringAsync($"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/binary/.dsync.txt");
			}
			catch (Exception ex)
			when (ex is HttpRequestException)
			{
				return true;
			}
			try
			{
				var items = status.Split(';').Select(x => x.Trim()).ToArray();
				var isUnderMaintainance = bool.Parse(items[0]);
				var isBlocking = bool.Parse(items[1]);
				var requiredVersion = new Version(items[2]);
				var requiredGeneration = int.Parse(items[3]);

				if (isUnderMaintainance)
					return true;

				if (isBlocking)
				{
					if (string.IsNullOrWhiteSpace(CachedAuthKey))
						return true;

					var isAcceptable = await Server.AuthorizeAsync(CachedAuthKey);
					if (!isAcceptable)
						return true;
				}

				var currentVersion = Versioning.VersionHistory.Latest;
				var currentGeneration = Versioning.Generation.Current;
				if (requiredVersion > currentVersion || requiredGeneration > currentGeneration)
					return true;

				return false;
			}
			catch
			{
				return true;
			}
		}
	}

	internal static async Task AlertIdDataAsync(HttpClient client, IJSRuntime jsRuntime, ILocalStorageService localStorage)
	{
		try
		{
			var spec = await Server.SpeculateAsync(localStorage);
			if (spec is null)
			{
				await jsRuntime.Alert("通信エラーが発生しました。しばらく時間を置いてからもう一度お試しください。");
				return;
			}

			if (!spec.Contains("不明"))
			{
				await jsRuntime.Alert(spec);
				return;
			}

			var name = await localStorage.GetItemAsync<string?>(LSKeys.UserName);
			var guid = await localStorage.GetItemAsync<string?>(LSKeys.UserId);
			var hash = await AppSettings.GetHashDirectlyAsync(client);

			if (name is null || guid is null || hash is null)
			{
				var tmphash = await AppSettings.TemporaryHashAsync(client);
				await jsRuntime.Alert($"Data: {{ Status: \"新規ユーザー\", TH: {tmphash} }}");
				return;
			}

			var guidTrim = guid.Take(6).StringJoin();
			var hashTrim = hash.Take(6).StringJoin();
			var data = $"Data: {{ Status: \"不明なユーザー\", Name: {name}, Id: g0{guidTrim}h0{hashTrim} }}";
			await jsRuntime.Alert(data);
		}
		catch (Exception ex)
		{
			await jsRuntime.AlertEx(ex);
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

	internal static bool IgnoreOnlineStatus { get; private set; } =
#if DEBUG
		false
#else
        false
#endif
		;
}

internal static class AppTitle
{
	internal static string Format(string title) => $"{title} - FirstLast4";
}

internal static class LSKeys
{
	internal const string
		UserName = "USER_NAME",
		UserId = "USER_ID",
		HashId = "HASH_ID",
		HashV4 = "HASH_V4",
		HashV6 = "HASH_V6",
		IsLoggedIn = "IS_LOGGED_IN",
		SortResult = "SORT_RESULT",
		UseExists = "USE_EXISTS",
		BetaMode = "BETA_MODE",
		VolatileMode = "VOLATILE_MODE",
		HasLoaded = "hasLoaded",
		TypedWords = "typedWords",
		ModuleFiles = "MODULE_FILES",
		CachedAuthKey = "AUTHKEY_CACHE";
}