using System.Diagnostics;
using System.Net;
using Blazored.LocalStorage;

namespace SBFirstLast4;

public static class WordDictionary
{
	public static List<string> NoTypeWords { get; internal set; } = new(3_000_000);
	public static List<Word> TypedWords { get; internal set; } = new(20_000);

	public static Word[] PerfectDic => _perfectDic ??= GeneratePerfectDic().ToArray();
	static Word[]? _perfectDic = null;
	static IEnumerable<Word> GeneratePerfectDic()
	{
		foreach (var i in NoTypeWords) yield return (Word)i;
		foreach (var i in TypedWords) yield return i;
	}


	public static string[] PerfectNameDic => _perfectNameDic ??= GeneratePerfectNameDic().ToArray();
	static string[]? _perfectNameDic = null;
	static IEnumerable<string> GeneratePerfectNameDic()
	{
		foreach (var i in NoTypeWords) yield return i;
		foreach (var i in TypedWords) yield return i.Name;
	}

	public static bool IsLoadedCorrectly => NoTypeWords.Count > 2_000_000 || TypedWords.Count > 10_000 || _loadSkip;
	public static bool IsLite => NoTypeWords.Count < 2_000_000;
	private static bool _loadSkip = false;

	private static readonly string[] _dummyData = new[]
	{
		"のーまる", "どうぶつ", "しょくぶつ", "ちめい", "かんじょう",
		"げいじゅつ", "たべもの", "ぼうりょく", "いりょう", "じんたい",
		"きかい", "りか", "さきのばし", "じんぶつ", "こうさく",
		"ふくしょく", "しゃかい", "あそび", "むし", "すうがく",
		"ずぼし", "しゅうきょう", "すぽーつ", "てんではなしにならねぇよ", "ものがたり"
	};

	static readonly List<List<Word>> SplitList = new();
	private static readonly HttpClient client = new();

	const string HAS_LOADED = "hasLoaded";
	const string TYPED_WORDS = "typedWords";
	public static async IAsyncEnumerable<string> Initialize(ILocalStorageService localStorage, DictionaryInitializationToken token)
	{
		if(token is DictionaryInitializationToken.Skip)
		{
			yield return "読み込みをスキップしています..."; 
			_loadSkip = true; 
			NoTypeWords.AddRange(_dummyData); 
			yield break;
		}

		await foreach (var i in LoadDataFromOnline(localStorage, token)) yield return i;
		yield return "読み込みを完了しています...";
	}
	static async IAsyncEnumerable<string> LoadDataFromOnline(ILocalStorageService localStorage, DictionaryInitializationToken token)
	{
		if(token is DictionaryInitializationToken.Full)
		{
			await foreach(var i in FullLoading(localStorage)) yield return i;
			yield break;
		}

		if(token is DictionaryInitializationToken.Lite)
		{
			await foreach (var i in LoadTypedWordsFromOnline(localStorage)) yield return i;
			yield return "リストを分割しています...";
			await Task.Run(InitSplitList);
			yield break;
		}
	}
	static async IAsyncEnumerable<string> FullLoading(ILocalStorageService localStorage)
	{
		await foreach (var i in LoadNoTypeWordsFromOnline()) yield return i;

		await foreach (var i in LoadTypedWordsFromOnline(localStorage)) yield return i;

		yield return "リストを分割しています...";
		await Task.Run(InitSplitList);
		yield return "タイプレス リストを分離しています...";
		await Task.Run(() =>
		{
			NoTypeWords = NoTypeWords.AsParallel().Except(TypedWords.AsParallel().Select(x => x.Name)).ToList();
		});
	}

	static async IAsyncEnumerable<string> LoadNoTypeWordsFromOnline()
	{
		yield return "タイプレス ワードを読み込んでいます...";

		var tasks = new List<Task>();

		for (var i = 0; i < 240; i++)
		{
			yield return $"タイプレス ワードを読み込んでいます... ({i}/240)";
			tasks.Add(ReadNoTypeWords(client, i));
			if (i % 42 == 0 || i == 239)
			{
				try
				{
					await Task.WhenAll(tasks);
				}
				catch
				{
					yield break;
				}
				tasks.Clear();
			}
		}
	}
	static async IAsyncEnumerable<string> LoadTypedWordsFromOnline(ILocalStorageService localStorage)
	{
		yield return "タイプ付き ワードを読み込んでいます...";
		if (await localStorage.GetItemAsync<bool>(HAS_LOADED))
		{
			yield return "キャッシュを読み込んでいます...";
			TypedWords = await localStorage.GetItemAsync<List<Word>>(TYPED_WORDS);
			yield break;
		}

		var tasks = new List<Task>();
		var typedCount = 0;
		while (typedCount < SBUtils.KanaListSpread.Length) // 67
		{
			tasks.Add(ReadTypedWords(client, SBUtils.KanaListSpread[typedCount]));
			if (typedCount % 10 == 0)
			{
				yield return $"タイプ付き ワードを読み込んでいます... ({typedCount / 10}/7)";
				try
				{
					await Task.WhenAll(tasks);
				}
				catch
				{
					yield break;
				}
				tasks.Clear();
			}
			typedCount++;
		}
		yield return "タイプ付き ワードを読み込んでいます... (7/7)";
		await Task.WhenAll(tasks);
		TypedWords = TypedWords.Distinct().ToList();
		yield return "キャッシュを保存しています...";
		await localStorage.SetItemAsync(TYPED_WORDS, TypedWords);
		await localStorage.SetItemAsync(HAS_LOADED, true);
	}
	static void InitSplitList()
	{
		foreach (var i in SBUtils.KanaListSpread) SplitList.Add(TypedWords.Where(x => x.Name.At(0) == i[0]).ToList());
	}
	public static List<Word> GetSplitList(int index) => SplitList[index];
	public static List<Word> GetSplitList(char startChar) => SplitList.At(SBUtils.KanaListSpread.ToList().IndexOf(startChar.ToString())) ?? Enumerable.Empty<Word>().ToList();

	static async Task ReadNoTypeWords(HttpClient client, int arg)
	{
		var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/ntplain/notype{arg}.csv";
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Accept", "application/json");
		request.Headers.Add("Accept-Charset", "utf-8");
		string? resBodyStr;
		HttpStatusCode resStatusCode;
		HttpResponseMessage response;
		try
		{
			response = await client.SendAsync(request);
			resBodyStr = await response.Content.ReadAsStringAsync();
			resStatusCode = response.StatusCode;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.Message);
			throw;
		}
		NoTypeWords.AddRange(resBodyStr.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).AsParallel());
	}
	static async Task ReadTypedWords(HttpClient client, string arg)
	{
		var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/plain/typed-words-{arg}.csv";
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Accept", "application/json");
		request.Headers.Add("Accept-Charset", "utf-8");
		string? resBodyStr;
		HttpStatusCode resStatusCode;
		HttpResponseMessage response;
		try
		{
			response = await client.SendAsync(request);
			resBodyStr = await response.Content.ReadAsStringAsync();
			resStatusCode = response.StatusCode;
		}
		catch
		{
			throw;
		}
		TypedWords.AddRange(resBodyStr.Split("\n")
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim().Split())
			.Select(x => new Word(x.At(0) ?? string.Empty, x.At(1)?.StringToType() ?? WordType.Empty, x.At(2)?.StringToType() ?? WordType.Empty)));
	}
}

public enum DictionaryInitializationToken
{
	Full, Lite, Skip
}


