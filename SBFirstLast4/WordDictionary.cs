using System.Net;

namespace SBFirstLast4;

public static class WordDictionary
{
	public static List<Word> PerfectDic { get; private set; } = new();
	public static List<string> NoTypeWords { get; internal set; } = new();
	public static List<Word> TypedWords { get; internal set; } = new();
	static readonly List<List<Word>> SplitList = new();
	public static IEnumerable<string> Initialize()
	{
		using var client = new HttpClient();
		for (var i = 0; i < 240; i++)
		{
			yield return $"notype{i}.csv を読み込み中...";
			ReadNoTypeWords(client, i);
		}
		foreach (var i in SBUtils.KanaListSpread)
		{
			yield return $"{i} から始まる単語を読み込み中...";
			ReadTypedWords(client, i);
		}
		yield return "Loading perfect dictionary...";
		InitPerfectDic();
		yield return "Initializing split lists...";
		InitSplitList();
		yield return "読み込みを完了しています...";
	}
	static void InitPerfectDic()
	{
		var result = new Dictionary<string, (WordType Type1, WordType Type2)>();
		foreach (var i in NoTypeWords) result.TryAdd(i, (WordType.Empty, WordType.Empty));
		foreach (var i in TypedWords) result[i.Name] = (i.Type1, i.Type2);
		PerfectDic = result.ToList().Select(x => new Word(x.Key, x.Value.Type1, x.Value.Type2)).ToList();
	}
	static void InitSplitList()
	{
		foreach (var i in SBUtils.KanaListSpread) SplitList.Add(TypedWords.Where(x => x.Name.At(0) == i.At(0)).ToList());
	}
	public static List<Word> GetSplitList(int index) => SplitList[index];
	public static List<Word> GetSplitList(char startChar) => SplitList[SBUtils.KanaListSpread.ToList().IndexOf(startChar.ToString())];

	static void ReadNoTypeWords(HttpClient client, int arg)
	{
		var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/ntplain/notype{arg}.csv";
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Accept", "application/json");
		request.Headers.Add("Accept-Charset", "utf-8");
		string? resBodyStr;
		HttpStatusCode resStatusCode;
		Task<HttpResponseMessage> response;
		try
		{
			response = client.SendAsync(request);
			resBodyStr = response.Result.Content.ReadAsStringAsync().Result;
			resStatusCode = response.Result.StatusCode;
		}
		catch
		{
			return;
		}
		NoTypeWords.AddRange(resBodyStr.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
	}
	static void ReadTypedWords(HttpClient client, string arg)
	{
		var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/plain/typed-words-{arg}.csv";
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Accept", "application/json");
		request.Headers.Add("Accept-Charset", "utf-8");
		string? resBodyStr;
		HttpStatusCode resStatusCode;
		Task<HttpResponseMessage> response;
		try
		{
			response = client.SendAsync(request);
			resBodyStr = response.Result.Content.ReadAsStringAsync().Result;
			resStatusCode = response.Result.StatusCode;
		}
		catch
		{
			return;
		}
		TypedWords.AddRange(resBodyStr.Split("\n")
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim().Split())
			.Select(x => new Word(x.At(0) ?? string.Empty, x.At(1)?.StringToType() ?? WordType.Empty, x.At(2)?.StringToType() ?? WordType.Empty)));
	}
}
