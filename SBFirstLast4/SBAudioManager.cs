using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4;

public static class SBAudioManager
{
	private static readonly Dictionary<string, IJSObjectReference> audioPlayers = new();
	public static async Task Initialize(IJSRuntime jsRuntime)
	{
		try
		{
			foreach (var i in new[]
			{
			"animal", "art", "barrier", "body", "bug", "cloth", "concent", "denno", "down", "effective", "emote", "end", "food", "heal", "health", "horizon", "insult", "last",
			"math", "mech", "middmg", "ninja", "noneffective", "normal", "overflow", "pera", "person", "place", "plant", "play", "poison", "poison_heal", "religion",
			"science", "seed_damage", "seeded", "society", "sports", "start", "tale", "time", "up", "violence", "warn", "weather", "wonderland", "work"
		})
			{
				audioPlayers.TryAdd(i, await CreatePlayerAsync(jsRuntime, $"audio/{i}.mp3"));
			}

			foreach (var i in new[] { "horizon", "overflow", "last", "ninja" })
			{
				await audioPlayers[i].SetProperty("volume", 0.28f);
				await audioPlayers[i].SetProperty("loop", true);
			}
			foreach (var i in new[] { "denno", "wonderland" })
			{
				await audioPlayers[i].SetProperty("volume", 0.36f);
				await audioPlayers[i].SetProperty("loop", true);
			}
			await audioPlayers["pera"].SetProperty("volume", 0.4f);
			await audioPlayers["art"].SetProperty("volume", 0.8f);
		}
		catch (Exception ex)
		{
			await jsRuntime.Alert(ex);
		}
	}

	private static async Task<IJSObjectReference> CreatePlayerAsync(IJSRuntime jsRuntime, string fileName)
	{
		try
		{
			var audio = await jsRuntime.InvokeAsync<IJSObjectReference>("eval", $"new Audio('{fileName}')");
			return audio;
		}
		catch (Exception ex)
		{
			await jsRuntime.Alert(ex);
			return null!;
		}
	}
	public static string[] AudioNames => audioPlayers.Keys.ToArray();
	public static async Task PlayAudio(string soundName)
	{
		if (audioPlayers.TryGetValue(soundName, out var player)) await player.InvokeVoidAsync("play");
	}
	public static async void PlayAudioForget(string soundName)
	{
		await PlayAudio(soundName);
	}
	public static async Task<bool> TryPlayAudio(string soundName)
	{
		var result = audioPlayers.TryGetValue(soundName, out var player);

		if (result)
			await (player?.InvokeVoidAsync("play") ?? ValueTask.CompletedTask);

		return result;
	}
	public static async Task PauseAudio(string soundName)
	{
		if (audioPlayers.TryGetValue(soundName, out var player)) await player.InvokeVoidAsync("pause");
	}
	public static async Task SeizeAudio(string soundName)
	{
		if (!audioPlayers.TryGetValue(soundName, out var player)) 
			return;

		await player.InvokeVoidAsync("pause");
		await player.SetProperty("currentTime", 0);
	}
	public static async Task CancelAudio()
	{
		foreach (var i in audioPlayers.Values) await i.InvokeVoidAsync("pause");
	}
	public static bool TryGetPlayer(string soundName, [NotNullWhen(true)] out IJSObjectReference? player) => audioPlayers.TryGetValue(soundName, out player);
}

