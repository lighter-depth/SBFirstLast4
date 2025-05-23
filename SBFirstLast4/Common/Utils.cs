﻿using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SBFirstLast4.Dynamic;
using SBFirstLast4.Pages;
using SBFirstLast4.Simulator;
using Index = System.Index;

namespace SBFirstLast4;

public static class Utils
{
	public static Random Random { get; set; } = new();

	public static string[][] KanaList => kanaList ??=
	[
		["あ", "い", "う", "え", "お"],
		["か", "き", "く", "け", "こ"],
		["さ", "し", "す", "せ", "そ"],
		["た", "ち", "つ", "て", "と"],
		["な", "に", "ぬ", "ね", "の"],
		["は", "ひ", "ふ", "へ", "ほ"],
		["ま", "み", "む", "め", "も"],
		["や", "ゆ", "よ"],
		["ら", "り", "る", "れ", "ろ"],
		["わ"],
		["が", "ぎ", "ぐ", "げ", "ご"],
		["ざ", "じ", "ず", "ぜ", "ぞ"],
		["だ", "で", "ど"],
		["ば", "び", "ぶ", "べ", "ぼ"],
		["ぱ", "ぴ", "ぷ", "ぺ", "ぽ"]
	];

	private static string[][]? kanaList;

	public static string[] KanaListSpread => _kanaListSpread ??= KanaList.SelectMany(x => x).ToArray();
	private static string[]? _kanaListSpread;

	public static char[] KanaListCharSpread => _kanaListCharSpread ??= KanaList.SelectMany(x => x).Select(x => x.At(0)).ToArray();
	private static char[]? _kanaListCharSpread;

	public static bool IsValidRegex(string pattern)
	{
		try
		{
			_ = new Regex(pattern);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static char GetLastChar(this string str)
	{
		var length = str.Length;

		if (length == 0)
			return default;

		var lastChar = str[length - 1];

		if (_siritoriChar.TryGetValue(lastChar, out var result))
			return result;

		var penultimateChar = length > 1 ? str[length - 2] : default;

		return lastChar switch
		{
			'ー' when _siritoriChar.TryGetValue(penultimateChar, out var result2) => result2,
			'ー' => penultimateChar,
			_ => lastChar
		};
	}

	private static readonly Dictionary<char, char> _siritoriChar = new()
	{
		['ゃ'] = 'や',
		['ゅ'] = 'ゆ',
		['ょ'] = 'よ',
		['っ'] = 'つ',
		['ぁ'] = 'あ',
		['ぃ'] = 'い',
		['ぅ'] = 'う',
		['ぇ'] = 'え',
		['ぉ'] = 'お',
		['を'] = 'お',
		['ぢ'] = 'じ',
		['づ'] = 'ず'
	};

	public static bool IsWild(this char c) => c is '*' or '＊';

	public static bool IsWild(this string name) => name.Contains('*') || name.Contains('＊');

	public static string ReplaceFreeChar(this string input, char oldValue, char newValue) => input.ReplaceFreeString(oldValue.ToString(), newValue.ToString());

	public static string ReplaceFreeString(this string input, string oldValue, string newValue)
	=> Regex.Replace(input, Regex.Escape(oldValue), m =>
	{
		if (Is.InsideStringLiteral(m.Index, m.Length, input))
			return m.Value;

		return newValue;
	});


	public static string Stringify(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

	public static bool IsDefault<T>(this T value) where T : struct => value.Equals(default(T));

	public static Type GetTypeOrDefault<T>(this T? obj) => obj?.GetType() ?? typeof(object);

	public static async Task DeleteItemAsync(this ILocalStorageService localStorage, string key)
	{
		await localStorage.SetItemAsync(key, default(object));
		await localStorage.RemoveItemAsync(key);
	}

	public static string Hash(string root)
		=> SHA256.HashData(Encoding.UTF8.GetBytes(root)).Select(b => b.ToString()).StringJoin();
}


public sealed class Wrapper<T>(T value)
{
	private T _value = value;

	public T Value
	{
		get => _value;
		set => _value = value;
	}

	public ref T RefValue => ref _value;

	public Wrapper<T> ShallowCopy() => new(_value);
}

public static class JSHelper
{
	public static ValueTask Alert(this IJSRuntime jsRuntime, params object?[]? args) => jsRuntime.InvokeVoidAsync("alert", args);

	public static ValueTask AlertEx(this IJSRuntime jsRuntime, Exception ex) => jsRuntime.InvokeVoidAsync("alert", ex.Stringify());

	public static ValueTask<bool> Confirm(this IJSRuntime jsRuntime, params object?[]? args) => jsRuntime.InvokeAsync<bool>("confirm", args);

	public static ValueTask<T> GetElementValueById<T>(this IJSRuntime jsRuntime, string id) => jsRuntime.InvokeAsync<T>("eval", $"document.getElementById('{id}').value");

	public static ValueTask SetElementValueById<T>(this IJSRuntime jSRuntime, string id, T value) => jSRuntime.InvokeVoidAsync("eval", $"document.getElementById('{id}').value = '{value}'");

	public static ValueTask ClearElementValueById(this IJSRuntime jsRuntime, string id) => jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{id}').value = ''");

	public static ValueTask<T> GetProperty<T>(this IJSObjectReference jsRef, string key) => jsRef.InvokeAsync<T>("getProperty", key);

	public static ValueTask SetProperty<T>(this IJSObjectReference jsRef, string key, T value) => jsRef.InvokeVoidAsync("setProperty", key, value);

	public static void Alert(this IJSInProcessRuntime jsRuntime, params object?[]? args) => jsRuntime.InvokeVoid("alert", args);

	public static void AlertEx(this IJSInProcessRuntime jsRuntime, Exception ex) => jsRuntime.InvokeVoid("alert", ex.Stringify());

	public static bool Confirm(this IJSInProcessRuntime jsRuntime, params object?[]? args) => jsRuntime.Invoke<bool>("confirm", args);

	public static T GetElementValueById<T>(this IJSInProcessRuntime jsRuntime, string id) => jsRuntime.Invoke<T>("eval", $"document.getElementById('{id}').value");

	public static void SetElementValueById<T>(this IJSInProcessRuntime jSRuntime, string id, T value) => jSRuntime.InvokeVoid("eval", $"document.getElementById('{id}').value = '{value}'");

	public static void ClearElementValueById(this IJSInProcessRuntime jsRuntime, string id) => jsRuntime.InvokeVoid("eval", $"document.getElementById('{id}').value = ''");

}

public static class NavHelper
{
	public static void GoTo(this NavigationManager nav, string location) => nav.NavigateTo(location, false);

	public static void ForceReload(this NavigationManager nav) => nav.NavigateTo(string.Empty, true);

	public static void Return(this NavigationManager nav) => nav.GoTo(Locations.Index);

	public static void GoToTop(this NavigationManager nav) => nav.GoTo(Locations.Top);
}

public static class CollectionHelper
{
	public static int IndexOf<T>(this T[] array, T value) => Array.IndexOf(array, value);

	public static int BinarySearch<T>(this T[] array, T value) => Array.BinarySearch(array, value);

	public static List<T> RemoveRange<T>(this List<T> list, IEnumerable<T> values) => list.Except(values).ToList();

	public static void ReplaceOrAdd<T>(this List<T> list, T value)
		where T : IEquatable<T>
	{
		var index = list.FindIndex(i => i.Equals(value));
		if (index < 0)
		{
			list.Add(value);
			return;
		}
		list[index] = value;
	}

	public static void Add(this List<AnnotatedString> list, string text, Notice notice) => list.Add(new(text, notice));

	public static void Add(this List<AnnotatedString> list, string text, Notice notice, params int[] args) => list.Add(new(text, notice) { Params = args });

	public static void Add(this List<AnnotatedString> list, Notice notice, int player1HP, int player2HP) => list.Add(new(string.Empty, notice) { Params = [player1HP, player2HP] });

	public static void Add(this List<AnnotatedString> list, Notice notice, BattleData data) => list.Add(new(string.Empty, notice) { Data = data });

	public static void Add<T>(this Stack<T> stack, T item) => stack.Push(item);

	public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);

	public static IEnumerable<Word> SortByLength(this IEnumerable<Word> words, SortArg arg)
	{
		var result = new List<Word>();
		if (arg == SortArg.NoConstraint) return words;
		for (var i = 7; i < 12; i++) result.AddRange(words.Where(x => x.Name.Length == i));
		result.AddRange(words.Where(x => x.Name.Length >= 12));
		if (arg == SortArg.OnlyMoreThanSeven) return result;
		result.AddRange(words.Where(x => x.Name.Length == 6).Select(x => x with { Name = $"({x.Name})" }));
		result.AddRange(words.Where(x => x.Name.Length < 6).Select(x => x with { Name = $"({x.Name})" }));
		return result;
	}
	public static IEnumerable<string> SortByLength(this IEnumerable<string> words, SortArg arg)
	{
		var result = new List<string>();
		if (arg == SortArg.NoConstraint) return words;
		for (var i = 7; i < 12; i++) result.AddRange(words.Where(x => x.Length == i));
		result.AddRange(words.Where(x => x.Length >= 12));
		if (arg == SortArg.OnlyMoreThanSeven) return result;
		result.AddRange(words.Where(x => x.Length == 6).Select(x => $"({x})"));
		result.AddRange(words.Where(x => x.Length < 6).Select(x => $"({x})"));
		return result;
	}

	public static T? At<T>(this IEnumerable<T> source, int index) => source.ElementAtOrDefault(index);

	public static T? At<T>(this IEnumerable<T> source, Index index) => source.ElementAtOrDefault(index);

	public static T? At<T>(this T[] source, int index) => index < 0 || index >= source.Length ? default : source[index];

	public static T? At<T>(this T[] source, Index index) => index.Value < 0 || index.Value >= source.Length ? default : source[index];

	public static T? At<T>(this IList<T> source, int index) => index < 0 || index >= source.Count ? default : source[index];

	public static T? At<T>(this IList<T> source, Index index) => index.Value < 0 || index.Value >= source.Count ? default : source[index];

	public static char At(this string source, int index) => index < 0 || index >= source.Length ? default : source[index];

	public static char At(this string source, Index index) => index.Value < 0 || index.Value >= source.Length ? default : source[index];

	public static TValue? AtKey<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) => source.TryGetValue(key, out var value) ? value : default;

	public static string StringJoin<T>(this IEnumerable<T> values, char separator) => string.Join(separator, values);

	public static string StringJoin<T>(this IEnumerable<T> values, string separator) => string.Join(separator, values);

	public static string StringJoin<T>(this IEnumerable<T> values) => string.Join(string.Empty, values);

	public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> source) => source.Select((x, i) => (x, i));

	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull => source.ToDictionary(kv => kv.Key, kv => kv.Value);

	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<ValueTuple<TKey, TValue>> source) where TKey : notnull => source.ToDictionary(t => t.Item1, t => t.Item2);


	public static IEnumerable<TSource> WhereWhen<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool condition)
	{
		if (!condition)
			return source;

		return source.Where(predicate);
	}
}

public static class StringHelper
{
	public static string[] SplitIfNotNull(this string str, string? separator)
		=> separator is null ? [str] : str.Split(separator);

	public static bool ContainsAny(this string str, params string[] values)
		=> values.Any(str.Contains);

	public static T? Parse<T>(this string? str) where T : IParsable<T> 
		=> T.TryParse(str, null, out var result) ? result : default;

	public static bool ParseBool(this string? str) => bool.TryParse(str, out var result) && result;
}

public sealed class Completion<T>
{
	private TaskCompletionSource<T> _completionSource = new();

	public Task<T> Task
	{
		get
		{
			_completionSource = new();
			return _completionSource.Task;
		}
	}

	public void SetResult(T result) => _completionSource.TrySetResult(result);
}