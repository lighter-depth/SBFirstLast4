using System.Text.RegularExpressions;

namespace SBFirstLast4.Syntax;

public static partial class ValidLetterSyntax
{
	public static Regex Regex => Words.IsLocal ? AnyLetters() : KanaLetters();

	public static Regex RegexWild => Words.IsLocal ? AnyLettersWild() : KanaLettersWild();

	public static Regex RegexConsole => Words.IsLocal ? AnyLetters() : KanaLettersConsole();

	public static IEnumerable<char> LetterList
	{
		get
		{
			if (!Words.IsLocal)
				return Utils.KanaListCharSpread;

			var letters = new HashSet<char>();
			foreach (var i in Words.PerfectNameDic)
				letters.Add(i.At(0));

			return letters;
		}
	}

	public static bool IsValid(char c) => Words.IsLocal || Utils.KanaListSpread.Contains(c.ToString());

	[GeneratedRegex(@"^\w+$")]
	private static partial Regex AnyLetters();

	[GeneratedRegex("^[ぁ-ゟー]+$")]
	private static partial Regex KanaLetters();

	[GeneratedRegex(@"^[*＊\w]+$")]
	private static partial Regex AnyLettersWild();

	[GeneratedRegex("^[*＊ぁ-ゟー]+$")]
	private static partial Regex KanaLettersWild();

	[GeneratedRegex("^[ぁ-ゔゟヴー]*$")]
	private static partial Regex KanaLettersConsole();
}
