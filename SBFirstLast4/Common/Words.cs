using Blazored.LocalStorage;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using SBFirstLast4.Shared;
using Progress = System.Func<string, System.Threading.Tasks.Task>;

namespace SBFirstLast4;

[DynamicLinqType]
public static class Words
{
	public static List<string> NoTypeWords { get; internal set; } = [];
	public static List<Word> TypedWords { get; internal set; } = [];

	public static Word[] PerfectDic => _perfectDic ??= GeneratePerfectDic().ToArray();
	private static Word[]? _perfectDic;
	private static IEnumerable<Word> GeneratePerfectDic()
	{
		foreach (var i in NoTypeWords) yield return (Word)i;
		foreach (var i in TypedWords) yield return i;
	}


	public static string[] PerfectNameDic => _perfectNameDic ??= GeneratePerfectNameDic().ToArray();
	private static string[]? _perfectNameDic;
	private static IEnumerable<string> GeneratePerfectNameDic()
	{
		foreach (var i in NoTypeWords) yield return i;
		foreach (var i in TypedWords) yield return i.Name;
	}


	public static Word[] WordNoTypeWords => _wordNoTypeWords ??= NoTypeWords.Select(x => (Word)x).ToArray();
	private static Word[]? _wordNoTypeWords;

	public static string[] TypedWordNames => _typedWordNames ??= TypedWords.Select(x => x.Name).ToArray();
	private static string[]? _typedWordNames;

	public static bool IsLoadedCorrectly => NoTypeWords.Count > 2_000_000 || TypedWords.Count > 10_000 || IsLocal || _loadSkip;
	public static bool IsLite => !IsLocal && NoTypeWords.Count < 2_000_000;
	private static bool _loadSkip;

	internal static bool IsLocal { get; set; } = false;

	private static readonly string[] _dummyData = [
		"のーまる", "どうぶつ", "しょくぶつ", "ちめい", "かんじょう",
		"げいじゅつ", "たべもの", "ぼうりょく", "いりょう", "じんたい",
		"きかい", "りか", "さきのばし", "じんぶつ", "こうさく",
		"ふくしょく", "しゃかい", "あそび", "むし", "すうがく",
		"ずぼし", "しゅうきょう", "すぽーつ", "てんではなしにならねぇよ", "ものがたり"
	];

	private static List<List<Word>> SplitList = [];

	private static Dictionary<char, List<Word>> SplitListDictionary = [];

	public static void Clear()
	{
		NoTypeWords = [];
		TypedWords = [];
		_perfectDic = null;
		_perfectNameDic = null;
		_wordNoTypeWords = null;
		_typedWordNames = null;
		SplitList = [];
		SplitListDictionary = [];
	}

	public static async Task Initialize(Progress progress, HttpClient client, ILocalStorageService localStorage, IWordLoaderService wordLoader, DictionaryInitializationToken token)
	{
		if (token is DictionaryInitializationToken.Skip)
		{
			await progress("読み込みをスキップしています...");
			_loadSkip = true;
			NoTypeWords.AddRange(_dummyData);
			return;
		}

		await progress("読み込みを開始しています...");

		await LoadDataFromOnline(progress, client, localStorage, wordLoader, token);
		await progress("読み込みを完了しています...");
	}

	private static async Task LoadDataFromOnline(Progress progress, HttpClient client, ILocalStorageService localStorage, IWordLoaderService wordLoader, DictionaryInitializationToken token)
	{
        TypedWords = new(20_000);

        if (token is DictionaryInitializationToken.Full)
		{
            NoTypeWords = new(3_000_000);
            await progress("完全版辞書を読み込んでいます...");

			await FullLoading(progress, client, localStorage, wordLoader);
			return;
		}

		if (token is DictionaryInitializationToken.Lite)
		{
			await progress("ライト版辞書を読み込んでいます...");

			await LoadTypedWordsFromOnline(progress, client, localStorage, wordLoader);
			await progress("リストを分割しています...");

			await Task.Run(InitSplitList);
		}
	}

	private static async Task FullLoading(Progress progress, HttpClient client, ILocalStorageService localStorage, IWordLoaderService wordLoader)
	{
		await LoadNoTypeWordsFromOnline(progress, client, wordLoader);

		await LoadTypedWordsFromOnline(progress, client, localStorage, wordLoader);

		await progress("リストを分割しています...");

		await Task.Run(InitSplitList);
		await progress("タイプレス リストを分離しています...");

		await Task.Run(ExceptDictionaries);
	}

	private static async Task LoadNoTypeWordsFromOnline(Progress progress, HttpClient client, IWordLoaderService wordLoader)
	{
		await progress("タイプレス ワードを読み込んでいます...");

		for (var i = 0; i < 6; i++)
		{
			var localParameter = i;
			await progress($"タイプレス ワードを読み込んでいます... ({i}/5)");
			await wordLoader.LoadTL(localParameter, client);
		}
	}

	private static async Task LoadTypedWordsFromOnline(Progress progress, HttpClient client, ILocalStorageService localStorage, IWordLoaderService wordLoader)
	{

		await progress("タイプ付き ワードを読み込んでいます...");
		if (await localStorage.GetItemAsync<bool>(LSKeys.HasLoaded))
		{
			await progress("キャッシュを読み込んでいます...");
			TypedWords = await localStorage.GetItemAsync<List<Word>>(LSKeys.TypedWords);
			return;
		}

		var tasks = new List<Task>();
		var typedCount = 0;
		while (typedCount < Utils.KanaListSpread.Length) // 67
		{
			var localParameter = Utils.KanaListSpread[typedCount];
			tasks.Add(wordLoader.LoadTD(localParameter, client));
			if (typedCount % 10 == 0)
			{
				await progress($"タイプ付き ワードを読み込んでいます... ({typedCount / 10}/7)");
				try
				{
					await Task.WhenAll(tasks);
				}
				catch
				{
					return;
				}
				tasks.Clear();
			}
			typedCount++;
		}
		await progress("タイプ付き ワードを読み込んでいます... (7/7)");
		await Task.WhenAll(tasks);
		TypedWords = TypedWords.AsEnumerable().Reverse().DistinctBy(w => w.Name).Reverse().ToList();
		await progress("キャッシュを保存しています...");
		await localStorage.SetItemAsync(LSKeys.TypedWords, TypedWords);
		await localStorage.SetItemAsync(LSKeys.HasLoaded, true);
	}
	private static void InitSplitList()
	{
		if (IsLocal)
		{
			SplitListDictionary = TypedWords.GroupBy(x => x.Start).ToDictionary(x => x.Key, x => x.ToList());
			return;
		}

		foreach (var i in Utils.KanaListSpread)
			SplitList.Add(TypedWords.Where(x => x.Name.At(0) == i[0]).ToList());
	}

	public static List<Word> GetSplitList(char startChar) 
	{
		if (IsLocal)
			return SplitListDictionary.AtKey(startChar) ?? [];

		return SplitList.At(Utils.KanaListSpread.ToList().IndexOf(startChar.ToString())) ?? []; 
	}

	private static void ExceptDictionaries() => NoTypeWords = NoTypeWords.AsParallel().Except(TypedWords.AsParallel().Select(x => x.Name)).ToList();

	public sealed class LocalLoader : IDisposable
	{
		public LocalLoader() => Clear();

		public LocalLoader Load(IEnumerable<string> words)
		{
			NoTypeWords.AddRange(words);
			return this;
		}

		public LocalLoader Load(IEnumerable<Word> words)
		{
			TypedWords.AddRange(words);
			return this;
		}

		public void Dispose() 
		{
			InitSplitList();
			ExceptDictionaries();
		}
	}
}

public enum DictionaryInitializationToken
{
	Full, Lite, Skip
}