using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Text;
using static SBFirstLast4.Dynamic.SelectorConstants;

namespace SBFirstLast4.Dynamic;

public static partial class Interpreter
{

	private static readonly Dictionary<char, char> EscapeCharacters = new()
	{
		['\u201c'] = '\"',
		['\u201d'] = '\"',
		['\u201e'] = '\"',
		['\uff02'] = '\"',
		['\u2018'] = '\'',
		['\u2019'] = '\'',
		['\uff07'] = '\''
	};



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
		["\\fc"] = $"{nameof(Extensions.DynamicExtensionHelper.FirstChar)}()",
		["\\lc"] = $"{nameof(Extensions.DynamicExtensionHelper.LastChar)}()",
		["\\c("] = $"{nameof(Word.Contains)}(",
		["\\cl("] = $"{nameof(Word.CalcEffectiveDmg)}(",
		["\\t2c("] = $"{nameof(WordTypeEx.TypeToChar)}(",
		["\\t2s("] = $"{nameof(WordTypeEx.TypeToString)}(",
		["\\c2t("] = $"{nameof(WordTypeEx.CharToType)}(",
		["\\s2t("] = $"{nameof(WordTypeEx.StringToType)}(",
		["Sort()"] = "OrderBy(it)",
		["Shuffle()"] = "OrderBy(Guid.NewGuid())",
		["Flatten()"] = "SelectMany(it)",
		["\\$("] = "string.Format(",
		["\\~("] = "IsMatch("
	};

	internal static bool IsAuto { get; set; } = true;

	internal static bool EasyArrayInitializer { get; set; } = true;


	public static bool TryInterpret(string input, [NotNullWhen(true)] out string? translated, [NotNullWhen(true)] out string? selector, [NotNullWhen(false)] out string? errorMsg)
	{
		if (!IsAuto) return TryInterpretManual(input, out translated, out selector, out errorMsg);

		(translated, selector, errorMsg) = (null, null, null);


		input = input.Trim();

		foreach (var (key, value) in EscapeCharacters)
			input = input.ReplaceFreeChar(key, value);

		input = input.ReplaceFreeString("@item", "it");

		var commentIndex = input.IndexOf("//");
		if (commentIndex != -1 && !IsInsideStringLiteral(commentIndex, 2, input)) input = input[..commentIndex];


		if (input.At(0) != '@')
			input = $"@SO.Select(x => {input}).First()";

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

		input = ReplaceGenericMethods(input);

		input = input[(selectorIndex + 1)..];


		/*if (input.Contains('['))
			input = ReplaceCollectionLiteral(input);*/

		if (input.Contains('{'))
			input = ReplaceArrayInitializer(input);

		if (input.Contains('`'))
			input = ReplaceRegexLiteral(input);

		if (input.Contains("/w"))
			input = ReplaceWordLiteral(input);

		if (input.Contains("/d"))
			input = ReplaceDeduceLiteral(input);

		if (input.Contains('$'))
			foreach (var (key, value) in WordTypeLiteral)
				input = input.ReplaceFreeString(key, value);

		if (input.Contains('@'))
			foreach (var (key, value) in DictionaryLiteral)
				input = input.ReplaceFreeString(key, value);

		if (input.Contains("++"))
			input = WideVariableRegex.Increment().Replace(input, m => WideVariable.GetIncrementString(m.Groups["name"].Value));

		if (input.Contains("--"))
			input = WideVariableRegex.Decrement().Replace(input, m => WideVariable.GetDecrementString(m.Groups["name"].Value));

		if (input.Contains('&'))
			input = WideVariableRegex.Reference().Replace(input, m => WideVariable.GetFormattedString(m.Groups["name"].Value));

		foreach (var (key, value) in ExtensionProperties)
			input = input.ReplaceFreeString(key, value);


		translated = input;

		return true;
	}


	private static bool TryInterpretManual(string input, [NotNullWhen(true)] out string? translated, [NotNullWhen(true)] out string? selector, [NotNullWhen(false)] out string? errorMsg)
	{
		(translated, selector, errorMsg) = (null, null, null);
		input = input.Trim();
		var selectorIndex = input.IndexOf('.');

		if (input.Length < 5 || selectorIndex < 0)
		{
			errorMsg = "Input must contain query body.";
			return false;
		}

		selector = input[..selectorIndex];

		if (!DictionaryLiteral.ContainsKey(selector))
		{
			errorMsg = $"Invalid dictionary selector: {input.AsSpan()[..selectorIndex]}";
			return false;
		}

		translated = input[(selectorIndex + 1)..];

		return true;
	}

	internal static string ExpandEphemeral(string input)
	{
		foreach (var macro in ModuleManager.Ephemerals.Reverse())
		{
			if (macro is FunctionLikeMacro functionLikeMacro)
			{
				input = Regex.Replace(input, $@"{functionLikeMacro.Name}\((?<parameters>[^)]+)\)", m =>
				{
					if (IsInsideStringLiteral(m.Index, m.Length, input))
						return m.Value;

					var args = m.Groups["parameters"].Value.Split(',').Select(arg => arg.Trim()).ToList();
					var body = functionLikeMacro.Body;
					for (var i = 0; i < functionLikeMacro.Parameters.Count; i++)
						body = body.Replace(functionLikeMacro.Parameters[i], args[i]);
					return body;
				});
				continue;
			}
			if (macro is ObjectLikeMacro objectLikeMacro)
				input = input.ReplaceFreeString(objectLikeMacro.Name, objectLikeMacro.Body);
		}
		return input;
	}

	internal static string ExpandMacro(string input)
	{
		foreach (var macro in ModuleManager.Macros.Reverse())
		{
			if (macro is FunctionLikeMacro functionLikeMacro)
			{
				input = Regex.Replace(input, $@"{functionLikeMacro.Name}\((?<parameters>[^)]+)\)", m =>
				{
					if (IsInsideStringLiteral(m.Index, m.Length, input))
						return m.Value;

					var args = m.Groups["parameters"].Value.Split(',').Select(arg => arg.Trim()).ToList();
					var body = functionLikeMacro.Body;
					for (var i = 0; i < functionLikeMacro.Parameters.Count; i++)
						body = body.Replace(functionLikeMacro.Parameters[i], args[i]);
					return body;
				});
				continue;
			}
			if (macro is ObjectLikeMacro objectLikeMacro)
				input = input.ReplaceFreeString(objectLikeMacro.Name, objectLikeMacro.Body);
		}
		return input;
	}

	private static string ReplaceGenericMethods(string input)
	{
		if (!input.Contains(".Cast<") && !input.Contains(".OfType<") && !input.Contains("static_cast<")) return input;

		var builder = new StringBuilder(input);

		foreach (var match in CastCallRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $".Cast(\"{match.Groups["type"].Value}\")");
		}

		foreach (var match in OfTypeCallRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $".OfType(\"{match.Groups["type"].Value}\")");
		}

		foreach (var match in StaticCastRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"{match.Groups["type"].Value}({match.Groups["operand"].Value})");
		}

		return builder.ToString();
	}


	private static string ReplaceCollectionLiteral(string input)
	{
		var sb = new StringBuilder();
		var index = 0;
		foreach (var match in CollectionLiteralRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Cast<Match>())
		{
			sb.Append(input.AsSpan(index, match.Index - index));
			var items = match.Groups["items"].Value;
			items = ReplaceCollectionLiteral(items);
			sb.Append("new[]{ " + items + " }");
			index = match.Index + match.Length;
		}
		sb.Append(input.AsSpan(index));
		return sb.ToString();
	}

	private static string ReplaceArrayInitializer(string input)
	{
		if (EasyArrayInitializer)
			return input.ReplaceFreeString("{", "new[]{");

		return input;
	}

	private static string ReplaceRegexLiteral(string input)
	{
		var builder = new StringBuilder(input);
		foreach (var match in RegexLiteralRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"\"{match.Groups["pattern"].Value}\".ToRegex()");
		}
		return builder.ToString();
	}
	private static string ReplaceWordLiteral(string input)
	{
		var builder = new StringBuilder(input);
		foreach (Match match in WordLiteralRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
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
	private static string ReplaceDeduceLiteral(string input)
	{
		var builder = new StringBuilder(input);
		foreach (var match in DeduceLiteralRegex().Matches(input).Where(m => !IsInsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"\"{match.Groups["name"].Value}\".Deduce()");
		}
		return builder.ToString();
	}

	internal static bool IsInsideStringLiteral(int startIndex, int length, string source)
	{
		if (startIndex < 0 || length < 0 || startIndex + length > source.Length)
			return false;

		var quoteCount = 0;
		for (var i = 0; i < startIndex; i++)
			if (source[i] == '"')
				quoteCount++;

		return quoteCount % 2 == 1;
	}

	[GeneratedRegex(@"`(?<pattern>.*?)`")]
	private static partial Regex RegexLiteralRegex();


	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*(?<type1>[A-Za-z])?(?<type2>[A-Za-z])?\s*/w")]
	private static partial Regex WordLiteralRegex();

	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*/d")]
	private static partial Regex DeduceLiteralRegex();

	[GeneratedRegex(@"(?<!\[)(?<!\])(?<!new)(?<!\))\[(?<items>(?:[^[\]]|\[[^[\]]*\])*)\]")]
	private static partial Regex CollectionLiteralRegex();

	[GeneratedRegex(@"\.Cast<(?<type>.*?)>\(\)")]
	private static partial Regex CastCallRegex();

	[GeneratedRegex(@"\.OfType<(?<type>.*?)>\(\)")]
	private static partial Regex OfTypeCallRegex();

	[GeneratedRegex(@"static_cast<(?<type>.*?)>\((?<operand>.*?)\)")]
	private static partial Regex StaticCastRegex();
}
