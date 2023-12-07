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
    public Word[] Search(Func<Word, bool>? predicate = null) => SBDictionary.PerfectDic.AsParallel().Where(predicate ?? Predicate).Where(x => Body.IsMatch(x.Name)).ToArray();
    public static Word[] SearchTyped(char startChar, Func<Word, bool> predicate) => SBDictionary.GetSplitList(startChar).Where(predicate).ToArray();

    public  Word[] SearchTyped(Func<Word, bool>? predicate = null, Func<Word, bool>? customLength = null) => SBDictionary.TypedWords.AsParallel().Where(predicate ?? Predicate).Where(customLength ?? (_ => true)).Where(x => Body.IsMatch(x.Name)).ToArray();
	public static string[] SearchFirstLast(char firstChar, char lastChar)
    {
        if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(^1) != 'ん').ToArray();
        if (firstChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.GetLastChar() == lastChar).ToArray();
        if (lastChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん').ToArray();
        return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar).ToArray();
    }
	public static string[] SearchFirstLast(char firstChar, char lastChar, Func<string, bool> pred)
	{
		if (firstChar is '*' or '＊' && lastChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(^1) != 'ん' && pred(x)).ToArray();
		if (firstChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.GetLastChar() == lastChar && pred(x)).ToArray();
		if (lastChar is '*' or '＊') return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.At(^1) != 'ん' && pred(x)).ToArray();
		return SBDictionary.PerfectNameDic.AsParallel().Where(x => x.At(0) == firstChar && x.GetLastChar() == lastChar && pred(x)).ToArray();
	}
    public static string[] SearchRegex(Regex regex) => SBDictionary.PerfectNameDic.AsParallel().Where(x => regex.IsMatch(x)).ToArray();
}

