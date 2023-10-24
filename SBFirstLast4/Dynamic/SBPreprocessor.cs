using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using static SBFirstLast4.Dynamic.SelectorConstants;

namespace SBFirstLast4.Dynamic;

public static partial class SBPreprocessor
{
	private static readonly Dictionary<string, string> ObjectMacros = new();



	private static readonly Dictionary<string, string> WordTypeLiteral
		=
		// $Sports => WordType.Sports
		Enum.GetNames<WordType>()
		.ToDictionary(name => $"${name}", name => $"{nameof(WordType)}.{name}")

		// $C => WordType.Art
		.Concat(
			Enum.GetValues<WordType>()
			.ToDictionary(type => $"${type.TypeToChar()}", type => $"{nameof(WordType)}.{Enum.GetName(type)!}")
			)

		// $r => WordType.Religion
		.Concat(
			Enum.GetValues<WordType>()
			.ToDictionary(type => $"${char.ToLower(type.TypeToChar())}", type => $"{nameof(WordType)}.{Enum.GetName(type)!}")
			)
		.ToDictionary(x => x.Key, x => x.Value);

	private static readonly Dictionary<string, string> DictionaryLiteral = new()
	{
		[NoTypeNames] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.NoTypeWords)}",
		[NoTypeWords] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.WordNoTypeWords)}",
		[TypedNames] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.TypedWordNames)}",
		[TypedWords] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.TypedWords)}",
		[PerfectWords] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.PerfectDic)}",
		[PerfectNames] = $"{nameof(SBDictionary)}.{nameof(SBDictionary.PerfectNameDic)}",
		[Killable] = $"{nameof(SBAuxLists)}.{nameof(SBAuxLists.Killable)}",
		[Semikillable] = $"{nameof(SBAuxLists)}.{nameof(SBAuxLists.SemiKillable)}",
		[Danger4] = $"{nameof(SBAuxLists)}.{nameof(SBAuxLists.Danger4)}",
		[CanBe4xed] = $"{nameof(SBAuxLists)}.{nameof(SBAuxLists.CanBe4xed)}",
		[Singleton] = $"{nameof(Extensions.DynamicExtensionHelper)}.{nameof(Extensions.DynamicExtensionHelper.GetSingleton)}()"
	};

	private static readonly Dictionary<string, string> ExtensionProperties = new()
	{
		[nameof(Extensions.DynamicExtensionHelper.FirstChar)] = $"{nameof(Extensions.DynamicExtensionHelper.FirstChar)}()",
		[nameof(Extensions.DynamicExtensionHelper.LastChar)] = $"{nameof(Extensions.DynamicExtensionHelper.LastChar)}()",
		[".\\C("] = $".{nameof(Word.Contains)}(",
		[".\\c("] = $".{nameof(Word.Contains)}(",
		[".\\Clc("] = $".{nameof(Word.CalcEffectiveDmg)}(",
		[".\\clc("] = $".{nameof(Word.CalcEffectiveDmg)}("
	};

	public static bool TryPreprocess(string input, [NotNullWhen(true)] out string[]? status, [NotNullWhen(false)] out string? errorMsg)
	{
		(status, errorMsg) = (null, null);
		input = input.Trim();
		if (input.Length < 2)
		{
			errorMsg = "The directive was empty.";
			return false;
		}
		input = input[(input.IndexOf('#') + 1)..];
		var symbol = input.Split().At(0);
		if (symbol is not ("define" or "undef" or "show" or "clear"))
		{
			errorMsg = "Couldn't recognize the specified directive type.";
			return false;
		}
		if (symbol is "show")
		{
			status = ObjectMacros.Select(x => $"Key: {x.Key}, Value: {x.Value}").ToArray();
			return true;
		}
		if (symbol is "clear")
		{
			ObjectMacros.Clear();
			status = new[] { "Cleared up the Macro Dictionary." };
			return true;
		}
		var contents = input.Split(' ');
		if (symbol is "undef")
		{
			if (contents.Length != 2)
			{
				errorMsg = "Invalid syntax: #undef syntax must have one argument.";
				return false;
			}
			if (!ObjectMacros.Remove(contents[1]))
			{
				errorMsg = $"Specified macro {contents[1]} does not exist in dictionary.";
				return false;
			}
			status = new[] { $"Successfully removed macro {contents[1]} from the dictionary." };
			return true;
		}

		if (contents.Length < 3)
		{
			errorMsg = "Invalid syntax: #define syntax must have two arguments.";
			return false;
		}
		var groups = DefineDirectiveRegex().Match(input).Groups;
		if (!ObjectMacros.TryAdd(groups["key"].Value, groups["value"].Value))
		{
			errorMsg = $"Specified macro {contents[1]} already exists in dictionary.";
			return false;
		}
		status = new[] { $"Successfully added macro {contents[1]} to the dictionary." };
		return true;
	}


	public static bool TryProcess(string input, [NotNullWhen(true)] out string? translated, [NotNullWhen(true)] out string? selector, [NotNullWhen(false)] out string? errorMsg)
	{
		(translated, selector, errorMsg) = (null, null, null);
		input = input.Trim();

		foreach (var (key, value) in ObjectMacros)
			input = input.Replace(key, value);


		if (input.At(0) != '@')
			input = $"@SO.Select(x => ({input})).First()";

		var selectorIndex = input.IndexOf('.');

		if (input.Length < 5 || selectorIndex < 0)
		{
			errorMsg = "Input must contain query body.";
			return false;
		}

		selector = input[..selectorIndex];

		if (!DictionaryLiteral.ContainsKey(selector))
		{
			errorMsg = $"Invalid dictionary selector: {input[..selectorIndex]}";
			return false;
		}

		input = input[(selectorIndex + 1)..];

		var collectionMatches = CollectionLiteralRegex().Matches(input);
		if (collectionMatches.Any(x => x.Success))
			input = ReplaceCollectionLiteral(input, collectionMatches);

		if (input.Contains('`'))
			input = ReplaceRegexLiteral(input);

		if (input.Contains("/w"))
			input = ReplaceWordLiteral(input);

		if (input.Contains('$'))
			foreach (var (key, value) in WordTypeLiteral)
				input = input.Replace(key, value);

		if (input.Contains('@'))
			foreach (var (key, value) in DictionaryLiteral)
				input = input.Replace(key, value);


		foreach (var (key, value) in ExtensionProperties)
			input = input.Replace(key, value);


		translated = input;

		return true;
	}
	private static string ReplaceCollectionLiteral(string input, MatchCollection matches)
	{
		var builder = new StringBuilder(input);
		foreach(var match in matches.Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"{match.Groups["start"].Value}new[]{{{match.Groups["content"].Value}}}{match.Groups["end"].Value}");
		}
		return builder.ToString();
	}
	private static string ReplaceRegexLiteral(string input)
	{
		var matches = RegexLiteralRegex().Matches(input);
		var builder = new StringBuilder(input);
		foreach (var match in matches.Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"\"{match.Groups["pattern"].Value}\".ToRegex()");
		}
		return builder.ToString();
	}
	private static string ReplaceWordLiteral(string input)
	{
		var matches = WordLiteralRegex().Matches(input);
		var builder = new StringBuilder(input);
		foreach (Match match in matches.Reverse())
		{
			var groups = match.Groups;
			var isEmpty = !groups["type1"].Success;
			var isSingleTyped = !groups["type2"].Success;

			var insert =
				isEmpty ? $"\"{groups["name"].Value}\".ToWord()"
				: isSingleTyped ? $"\"{groups["name"].Value}\".ToWord(${groups["type1"].Value})"
				: $"\"{groups["name"].Value}\".ToWord(${groups["type1"].Value}, ${groups["type2"].Value})";


			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, insert);
		}
		return builder.ToString();
	}

	[GeneratedRegex(@"define\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	private static partial Regex DefineDirectiveRegex();

	[GeneratedRegex(@"(?<start>^|[^\w^\]^w])\s*\[(?<content>.*?)\]\s*(?<end>$|[<>=(){}?:&|^~!+\-*/\\.,;\[\]])")]
	private static partial Regex CollectionLiteralRegex();


	[GeneratedRegex(@"`(?<pattern>.*?)`")]
	private static partial Regex RegexLiteralRegex();


	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*(?<type1>[A-Za-z])?(?<type2>[A-Za-z])?\s*/w")]
	private static partial Regex WordLiteralRegex();


#if false
#pragma warning disable
	static void Test()
	{


		// WordType リテラル (ドルマーク)

		var sports = "$Sports"; // スポーツ タイプ
		var abbr = "$C"; // 芸術 タイプ
		var abbr2 = "$r"; // 宗教 タイプ


		// 辞書セレクター (アットマーク)

		var notype = "@NN"; // 無属性辞書 ( List<string> )
		var typed = "@TW"; // 有属性辞書 ( List<Word> )
		var danger4 = "@D4"; // ４注辞書 ( List<string> )


		// Word リテラル (スラッシュ + 'w'サフィックス)

		var nureginu = "/ぬれぎぬ/w";
		var aiiro = "/あいいろ C/w";
		var nuka = "/ぬか KF/w";


		// Word リテラル (タイプ名直接表記、 'wr' サフィックス)

		var kusuri = "/くすり 医療/wr";
		var ujimushi = "/うじむし 虫 暴言/wr";


	}
#endif

}
