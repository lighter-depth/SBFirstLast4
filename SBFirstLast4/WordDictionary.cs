#pragma warning disable IDE1006, CS0162, IDE0051, IDE0052, IDE0044, IDE0060

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using Blazored.LocalStorage;
using Magic.IndexedDb;
using Magic.IndexedDb.SchemaAnnotations;

namespace SBFirstLast4;

public static class WordDictionary
{
	public static List<string> NoTypeWords { get; internal set; } = new(3_000_000);
	public static List<Word> TypedWords { get; internal set; } = new(20_000);
	public static bool IsLoadedCorrectly => NoTypeWords.Count > 2_000_000 || _loadSkip;
	private static bool _loadSkip = false;

	private static readonly string[] _dummyData = new[]
	{
		"のーまる", "どうぶつ", "しょくぶつ", "ちめい", "かんじょう",
		"げいじゅつ", "たべもの", "ぼうりょく", "いりょう", "じんたい",
		"きかい", "りか", "さきのばし", "じんぶつ", "こうさく",
		"ふくしょく", "しゃかい", "あそび", "むし", "すうがく",
		"ずぼし", "しゅうきょう", "すぽーつ", "てんではなしにならねぇよ", "ものがたり"
	};

	public static IEnumerable<Word> PerfectDic()
	{
		foreach (var i in NoTypeWords) yield return (Word)i;
		foreach (var i in TypedWords) yield return i;
	}
	public static IEnumerable<string> PerfectNameDic()
	{
		foreach (var i in NoTypeWords) yield return i;
		foreach (var i in TypedWords) yield return i.Name;
	}

	static readonly List<List<Word>> SplitList = new();
	private static readonly HttpClient client = new();

	const string HAS_LOADED = "hasLoaded";
	public static async IAsyncEnumerable<string> Initialize(ILocalStorageService localStorage, IMagicDbFactory magicDb)
	{
		//yield return "読み込みをスキップしています..."; _loadSkip = true; NoTypeWords.AddRange(_dummyData);  yield break;
		await localStorage.ClearAsync();
		var hasLoaded = false;//await localStorage.GetItemAsync<bool>(HAS_LOADED);
		if (!hasLoaded)
		{
			await foreach (var i in LoadDataFromOnline()) yield return i;
			//await foreach(var i in CacheDataToIndexedDb(magicDb)) yield return i;
			//await localStorage.SetItemAsync(HAS_LOADED, true);
		}
		else
		{
			//await foreach (var i in LoadDataFromIndexedDb(magicDb)) yield return i;
		}
		yield return "読み込みを完了しています...";
	}
	static async IAsyncEnumerable<string> LoadDataFromOnline()
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

		yield return "タイプ付き ワードを読み込んでいます...";
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
		yield return "リストを分割しています...";
		//await Task.Run(InitSplitList);
		yield return "タイプレス リストを分離しています...";
		await Task.Run(() =>
		{
			NoTypeWords = NoTypeWords.Except(TypedWords.Select(x => x.Name).AsParallel()).AsParallel().ToList();
		});
	}
	static async IAsyncEnumerable<string> CacheDataToIndexedDb(IMagicDbFactory magicDb)
	{
		yield return "キャッシュを保存しています...";
		var manager = await magicDb.GetDbManager(SBUtils.DB_NAME);
		yield return "キャッシュをクリアしています...";
		await manager.ClearTable(SBUtils.DB_NAME);
		yield return "ソースを分割しています...";
		var splits = NoTypeWords.SplitToChunks(10000).ToArray();
		var total = splits.Length;
		yield return $"キャッシュを保存しています... (0/{total})";
		var tasks = new List<Task>();
		for(var i = 0; i < total; i++)
		{
			yield return $"キャッシュを保存しています... ({i + 1}/{total})";
			try
			{
				tasks.Add(manager.AddRange(new[] { new WordModel { Value = splits[i] } }));
				if (i % 42 == 0 || i == total - 1)
				{
					await Task.WhenAll(tasks);
					tasks.Clear();
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}
	}
	static async IAsyncEnumerable<string> LoadDataFromIndexedDb(IMagicDbFactory magicDb)
	{
		yield return "キャッシュを読み込んでいます...";
		var manager = await magicDb.GetDbManager(SBUtils.DB_NAME);
		var splits = (await manager.GetAll<WordModel>()).ToArray();
		//var splits = (await manager.GetAll<WordModel>()).ToList();
		var total = splits.Length;
		for(var i = 0; i < total; i++)
		{
			yield return $"キャッシュを読み込んでいます... ({i + 1}/{total})";
			NoTypeWords.AddRange(splits[i].Value);
		}
	}
	static void InitSplitList()
	{
		foreach (var i in SBUtils.KanaListSpread) SplitList.Add(TypedWords.Where(x => x.Name.At(0) == i.At(0)).ToList());
	}
	public static List<Word> GetSplitList(int index) => SplitList[index];
	public static List<Word> GetSplitList(char startChar) => SplitList[SBUtils.KanaListSpread.ToList().IndexOf(startChar.ToString())];

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
[MagicTable("WordModel", SBUtils.DB_NAME)]
public record WordModel
{
	[MagicPrimaryKey("id")]
	public int _Id { get; set; }
	[MagicIndex("Value")]
	public required string[] Value { get; init; }
}


