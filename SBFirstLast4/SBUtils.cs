using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SBFirstLast4.Simulator;

namespace SBFirstLast4;

public static class SBUtils
{
	public static Random Random { get; set; } = new();

	public static string[][] KanaList => kanaList ??= new[]
	{
		new[]{ "あ", "い", "う", "え", "お" },
		new[]{ "か", "き", "く", "け", "こ"},
		new[]{"さ", "し", "す", "せ", "そ"},
		new[]{"た", "ち", "つ", "て", "と"},
		new[]{"な", "に", "ぬ", "ね", "の"},
		new[]{"は", "ひ", "ふ", "へ", "ほ"},
		new[]{"ま", "み", "む", "め", "も"},
		new[]{"や", "ゆ", "よ"},
		new[]{"ら", "り", "る", "れ", "ろ"},
		new[]{"わ"},
		new[]{"が", "ぎ", "ぐ", "げ", "ご"},
		new[]{"ざ", "じ", "ず", "ぜ", "ぞ"},
		new[]{"だ", "で", "ど"},
		new[]{"ば", "び", "ぶ", "べ", "ぼ"},
		new[]{"ぱ", "ぴ", "ぷ", "ぺ", "ぽ"}
	};
	static string[][]? kanaList;

	public static string[] KanaListSpread => kanaListSpread ??= KanaList.SelectMany(x => x).ToArray();
	static string[]? kanaListSpread;
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? At<T>(this IEnumerable<T> source, int index) => source.ElementAtOrDefault(index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? At<T>(this IEnumerable<T> source, Index index) => source.ElementAtOrDefault(index);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char At(this string source, int index) => index < 0 || index >= source.Length ? default : source[index];
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char At(this string source, Index index) => index.Value < 0 || index.Value >= source.Length ? default : source[index];

	public static char GetLastChar(this string str)
	{
		var length = str.Length;

		if (length == 0)
			return default;

		var lastChar = str[length - 1];

		if (siritoriChar.TryGetValue(lastChar, out var result))
			return result;

		var penultimateChar = length > 1 ? str[length - 2] : default;

		return lastChar switch
		{
			'ー' when siritoriChar.TryGetValue(penultimateChar, out var result2) => result2,
			'ー' => penultimateChar,
			_ => lastChar
		};
	}
	static readonly Dictionary<char, char> siritoriChar = new()
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);
	public static List<string[]> SplitToChunks(this List<string> source, int chunkSize)
	{
		const int chunksize = 10000;
		var sourceSpan = source.AsSpan();
		var chunks = new List<string[]>((int)Math.Ceiling((double)source.Count / chunkSize));
		for (var i = 0; i < source.Count; i += chunksize)
		{
			var count = Math.Min(chunksize, source.Count - i);
			chunks.Add(sourceSpan.Slice(i, count).ToArray());
		}
		return chunks;
	}

	public static bool IsWild(this char c) => c is '*' or '＊';

	public static bool IsWild(this string name) => name.Contains('*') || name.Contains('＊');

	public static void Add(this List<AnnotatedString> list, string text, Notice notice)
	{
		list.Add(new(text, notice));
	}
	public static void Add(this List<AnnotatedString> list, string text, Notice notice, params int[] args)
	{
		list.Add(new(text, notice) { Params = args });
	}
	public static void Add(this List<AnnotatedString> list, Notice notice, int player1HP, int player2HP)
	{
		list.Add(new(string.Empty, notice) { Params = new[] { player1HP, player2HP } });
	}
	public static void AddMany(this List<AnnotatedString> list, IEnumerable<AnnotatedString> msgs)
	{
		foreach (var msg in msgs)
			list.Add(msg);
	}

	public static ValueTask Alert(this IJSRuntime jsRuntime, params object?[]? args) => jsRuntime.InvokeVoidAsync("alert", args);

	public static ValueTask AlertEx(this IJSRuntime jsRuntime, Exception ex) => jsRuntime.InvokeVoidAsync("alert", ex.Stringify());

	public static ValueTask<bool> Confirm(this IJSRuntime jsRuntime, params object?[]? args) => jsRuntime.InvokeAsync<bool>("confirm", args);

	public static string Stringify(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

	public static void ReplaceOrAdd<T>(this List<T> list, T value)
		where T : IEquatable<T>
	{
		var index = list.FindIndex(i => i.Equals(value));
		if(index < 0)
		{
			list.Add(value);
			return;
		}
		list[index] = value;
	}
}