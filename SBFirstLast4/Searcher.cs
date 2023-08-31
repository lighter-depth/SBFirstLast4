using System.Text.RegularExpressions;

namespace SBFirstLast4;

public class Searcher
{
    public WordType? Type1 { get; init; } = null;
    public WordType? Type2 { get; init; } = null;
    public Regex Body { get; init; } = new(".*");
    public bool IsTypedOnly { get; init; } = false;
    public bool IsSingleTypedOnly { get; init; } = false;
    public bool IsDoubleTypedOnly { get; init; } = false;
    Func<Word, bool> Predicate => (Type1, Type2) switch
    {
        (WordType.Empty, WordType.Empty) => x => x.IsEmpty,
        (not null, not null) => x => x.Contains((WordType)Type1) && x.Contains((WordType)Type2),
        (not null, null) => x => x.Contains((WordType)Type1) && (!IsSingleTypedOnly || x.IsSingleType) && (!IsDoubleTypedOnly || x.IsDoubleType),
        (null, not null) => x => x.Contains((WordType)Type2) && (!IsSingleTypedOnly || x.IsSingleType) && (!IsDoubleTypedOnly || x.IsDoubleType),
        (null, null) when IsTypedOnly => x => !x.IsEmpty,
        (null, null) when IsSingleTypedOnly => x => x.IsSingleType,
        (null, null) when IsDoubleTypedOnly => x => x.IsDoubleType,
        _ => x => true
    };
    public List<Word> Search(Func<Word, bool>? predicate = null) => WordDictionary.PerfectDic().Where(predicate ?? Predicate).Where(x => Body.IsMatch(x.Name)).ToList();
    public static List<Word> SearchTyped(char startChar, Func<Word, bool> predicate) => WordDictionary.GetSplitList(startChar).Where(predicate).ToList();
    public static List<string> SearchFirstLast(char firstChar, char lastChar)
    {
        if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.At(^1) != 'ん').ToList();
        if (firstChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.GetLastChar() == lastChar).ToList();
        if (lastChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん').ToList();
        return WordDictionary.PerfectNameDic().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar).ToList();
    }
	public static List<string> SearchFirstLast(char firstChar, char lastChar, Func<string, bool> pred)
	{
		if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.At(^1) != 'ん' && pred(x)).ToList();
		if (firstChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.GetLastChar() == lastChar && pred(x)).ToList();
		if (lastChar is '*' or '＊') return WordDictionary.PerfectNameDic().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん' && pred(x)).ToList();
		return WordDictionary.PerfectNameDic().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar && pred(x)).ToList();
	}
}

