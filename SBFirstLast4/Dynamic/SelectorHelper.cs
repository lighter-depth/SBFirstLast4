using static SBFirstLast4.Dynamic.SelectorConstants;

namespace SBFirstLast4.Dynamic;

public static class SelectorHelper
{
	public static IEnumerable<string> ToStringEnumerable(string selector) => selector switch
	{
		NoTypeNames => SBDictionary.NoTypeWords,
		TypedNames => SBDictionary.TypedWordNames,
		PerfectNames => SBDictionary.PerfectNameDic,
		Killable => SBAuxLists.Killable,
		Semikillable => SBAuxLists.SemiKillable,
		Danger4 => SBAuxLists.Danger4,
		CanBe4xed => SBAuxLists.CanBe4xed,
		_ => throw new ArgumentException($"Invalid dictionary selector: {selector}")
	};

	public static IEnumerable<Word> ToWordEnumerable(string selector) => selector switch
	{
		NoTypeWords => SBDictionary.WordNoTypeWords,
		TypedWords => SBDictionary.TypedWords,
		PerfectWords => SBDictionary.PerfectDic,
		_ => throw new ArgumentException($"Invalid dictionary selector: {selector}")
	};


	public static DictionaryType GetDictionaryType(string selector) => selector switch
	{
		NoTypeWords or TypedWords or PerfectWords => DictionaryType.Word,
		NoTypeNames or TypedNames or PerfectNames or Killable or Semikillable or Danger4 or CanBe4xed => DictionaryType.String,
		_ => DictionaryType.None
	};
}

public static class SelectorConstants
{
	public const string DictionaryPrefix = "@";
	public const string NoTypeNames = DictionaryPrefix + "NN";
	public const string NoTypeWords = DictionaryPrefix + "NW";
	public const string TypedNames = DictionaryPrefix + "TN";
	public const string TypedWords = DictionaryPrefix + "TW";
	public const string PerfectNames = DictionaryPrefix + "PN";
	public const string PerfectWords = DictionaryPrefix + "PW";
	public const string Killable = DictionaryPrefix + "8X";
	public const string Semikillable = DictionaryPrefix + "6X";
	public const string Danger4 = DictionaryPrefix + "D4";
	public const string CanBe4xed = DictionaryPrefix + "4X";
	public const string Singleton = DictionaryPrefix + "SO";
}

public enum DictionaryType
{
	None, String, Word
}
