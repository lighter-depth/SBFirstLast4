using static SBFirstLast4.Dynamic.SelectorConstants;

namespace SBFirstLast4.Dynamic;

public static class SelectorHelper
{
	public static IEnumerable<string> ToStringEnumerable(string selector) => selector switch
	{
		NoTypeNames => Words.NoTypeWords,
		TypedNames => Words.TypedWordNames,
		PerfectNames => Words.PerfectNameDic,
		Killable => AuxLists.Killable,
		Semikillable => AuxLists.SemiKillable,
		Danger4 => AuxLists.Danger4,
		CanBe4xed => AuxLists.CanBe4xed,
		_ => throw new ArgumentException($"Invalid dictionary selector: {selector}")
	};

	public static IEnumerable<Word> ToWordEnumerable(string selector) => selector switch
	{
		NoTypeWords => Words.WordNoTypeWords,
		TypedWords => Words.TypedWords,
		PerfectWords => Words.PerfectDic,
		_ => throw new ArgumentException($"Invalid dictionary selector: {selector}")
	};


	public static DictionaryType GetDictionaryType(string? selector) => selector switch
	{
		NoTypeWords or TypedWords or PerfectWords => DictionaryType.Word,
		NoTypeNames or TypedNames or PerfectNames or Killable or Semikillable or Danger4 or CanBe4xed => DictionaryType.String,
		_ => DictionaryType.None
	};
}

public static class SelectorConstants
{
	public const string DictionaryPrefix = "@",
						NoTypeNames = DictionaryPrefix + "NN",
						NoTypeWords = DictionaryPrefix + "NW",
						TypedNames = DictionaryPrefix + "TN",
						TypedWords = DictionaryPrefix + "TW",
						PerfectNames = DictionaryPrefix + "PN",
						PerfectWords = DictionaryPrefix + "PW",
						Killable = DictionaryPrefix + "8X",
						Semikillable = DictionaryPrefix + "6X",
						Danger4 = DictionaryPrefix + "D4",
						CanBe4xed = DictionaryPrefix + "4X",
						Singleton = DictionaryPrefix + "SO";
}

public enum DictionaryType
{
	None, String, Word
}
