using SBFirstLast4.Pages;
using System.Text.RegularExpressions;

namespace SBFirstLast4;

public sealed partial class Searcher
{
    public WordType? Type1 { get; init; } = null;
    public WordType? Type2 { get; init; } = null;
    public Regex Body { get; init; } = DefaultRegex();
    public bool IsTypedOnly { get; init; } = false;
    public bool IsSingleTypedOnly { get; init; } = false;
    public bool IsDoubleTypedOnly { get; init; } = false;
    private Func<Word, bool> Predicate => (Type1, Type2) switch
    {
        (WordType.Empty, WordType.Empty) => x => x.IsEmpty,
        (not null, not null) => x => x.Contains((WordType)Type1) && x.Contains((WordType)Type2),
        (not null, null) => x => x.Contains((WordType)Type1) && (!IsSingleTypedOnly || x.IsSingleType) && (!IsDoubleTypedOnly || x.IsDoubleType),
        (null, not null) => x => x.Contains((WordType)Type2) && (!IsSingleTypedOnly || x.IsSingleType) && (!IsDoubleTypedOnly || x.IsDoubleType),
		(null, null) when IsSingleTypedOnly => x => x.IsSingleType,
		(null, null) when IsDoubleTypedOnly => x => x.IsDoubleType,
		(null, null) when IsTypedOnly => x => !x.IsEmpty,
        _ => _ => true
    };
    public Word[] Search(Func<Word, bool>? predicate = null) => Words.PerfectDic.AsParallel().Where(predicate ?? Predicate).Where(x => Body.IsMatch(x.Name)).ToArray();
    public static Word[] SearchTyped(char startChar, Func<Word, bool> predicate) => Words.GetSplitList(startChar).Where(predicate).ToArray();

    public  Word[] SearchTyped(Func<Word, bool>? predicate = null, Func<Word, bool>? customLength = null) => Words.TypedWords.AsParallel().Where(predicate ?? Predicate).Where(customLength ?? (_ => true)).Where(x => Body.IsMatch(x.Name)).ToArray();
	public static string[] SearchFirstLast(char firstChar, char lastChar)
    {
        if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.At(^1) != 'ん').ToArray();
        if (firstChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.GetLastChar() == lastChar).ToArray();
        if (lastChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん').ToArray();
        return Words.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar).ToArray();
    }
	public static string[] SearchFirstLast(char firstChar, char lastChar, Func<string, bool> pred)
	{
		if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.At(^1) != 'ん' && pred(x)).ToArray();
		if (firstChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.GetLastChar() == lastChar && pred(x)).ToArray();
		if (lastChar is '*' or '＊') return Words.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん' && pred(x)).ToArray();
		return Words.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar && pred(x)).ToArray();
	}
    public static string[] SearchRegex(Regex regex) => Words.PerfectNameDic.AsParallel().Where(x => regex.IsMatch(x)).ToArray();
	
    [GeneratedRegex(".*")]
	private static partial Regex DefaultRegex();
}

public static class SearchOptions
{
    public static void SetTL(char first, char last, Func<string, bool>? length, Regex? regex, ListDeclType type)
    {
        SearchResult.IsTyped = false;
        DownloadConfig.IsNotype = true;
        SearchResult.FirstChar = first;
        SearchResult.LastChar = last;
        SearchResult.CustomLengthPred = length;
        SearchResult.RegexBody = regex;
        DownloadConfig.DeclType = type;
    }

    public static void SetTD(Searcher searcher, Func<Word, bool>? length = null, Func<Word, bool>? predicate = null)
    {
        SearchResult.IsTyped = true;
        DownloadConfig.IsNotype = false;
        DownloadConfig.DeclType = ListDeclType.TypedOnly;
        SearchResult.TypedSearcher = searcher;
        SearchResult.CustomLengthPredTyped = length;
        SearchResult.TypedPredicate = predicate;
    }

    public static void SetTree(bool isTD, string query)
    {
		TreeSearchResult.Query = query;
		TreeSearchResult.IsTyped = isTD;
		if (isTD) SetTD(null!);
		else SetTL(default, default, null, null, ListDeclType.Last);
	}
}