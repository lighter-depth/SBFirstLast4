using System.Net;
using Blazored.LocalStorage;

namespace SBFirstLast4;

public static class WordDictionary
{
	public static List<Word> PerfectDic { get; private set; } = new();
	public static List<string> PerfectNameDic { get; private set; } = new();
	public static List<string> NoTypeWords { get; internal set; } = new();
	public static List<Word> TypedWords { get; internal set; } = new();
	static readonly List<List<Word>> SplitList = new();
	public static async IAsyncEnumerable<string> Initialize(ILocalStorageService localStorage)
	{
		//yield return "読み込みをスキップしています..."; yield break;
		using var client = new HttpClient();
		yield return "Loading no type words...";
		var tasks = new List<Task>();
		for (var i = 0; i < 240; i++)
		{
			yield return $"Loading no type words... ({i}/240)";
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
		yield return "Loading typed words...";
		var typedCount = 0;
		while (typedCount < SBUtils.KanaListSpread.Length) // 67
		{
			tasks.Add(ReadTypedWords(client, SBUtils.KanaListSpread[typedCount]));
			if (typedCount % 10 == 0)
			{
				yield return $"Loading typed words... ({typedCount / 10}/7)";
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
		yield return "Loading typed words... (7/7)";
		await Task.WhenAll(tasks);
		yield return "Loading perfect dictionary...";
		await Task.Run(InitPerfectDic);
		yield return "Loading perfect name dictionary...";
		await Task.Run(InitPerfectNameDic);
		yield return "Initializing split lists...";
		await Task.Run(InitSplitList);
		yield return "読み込みを完了しています...";
	}
	static void InitPerfectDic()
	{
		PerfectDic = new List<Word>().Concat(NoTypeWords.Select(x => (Word)x)).Concat(TypedWords).DistinctBy(x => x.Name).ToList();
	}
	static void InitPerfectNameDic()
	{
		PerfectNameDic = PerfectDic.Select(x => x.Name).ToList();
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
		catch
		{
			throw;
		}
		NoTypeWords.AddRange(resBodyStr.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
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
