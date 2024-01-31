using SBFirstLast4.Pages;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
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
		[NoTypeNames] = $"{nameof(Words)}.{nameof(Words.NoTypeWords)}",
		[NoTypeWords] = $"{nameof(Words)}.{nameof(Words.WordNoTypeWords)}",
		[TypedNames] = $"{nameof(Words)}.{nameof(Words.TypedWordNames)}",
		[TypedWords] = $"{nameof(Words)}.{nameof(Words.TypedWords)}",
		[PerfectWords] = $"{nameof(Words)}.{nameof(Words.PerfectDic)}",
		[PerfectNames] = $"{nameof(Words)}.{nameof(Words.PerfectNameDic)}",
		[Killable] = $"{nameof(AuxLists)}.{nameof(AuxLists.Killable)}",
		[Semikillable] = $"{nameof(AuxLists)}.{nameof(AuxLists.SemiKillable)}",
		[Danger4] = $"{nameof(AuxLists)}.{nameof(AuxLists.Danger4)}",
		[CanBe4xed] = $"{nameof(AuxLists)}.{nameof(AuxLists.CanBe4xed)}",
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


	public static async Task<(bool Success, string? Translated, string? Selector, string? ErrorMsg)> TryInterpretAsync(string input)
	{
		(string? translated, string? selector, string? errorMsg) = (null, null, null);
		if (!IsAuto)
		{
			var result = TryInterpretManual(input, out translated, out selector, out errorMsg);
			return (result, translated, selector, errorMsg);
		}

		(translated, selector, errorMsg) = (null, null, null);

		input = input.Trim();

		input = ReplaceFreeChars(input);

		input = input.ReplaceFreeString("@item", "it");

		var commentIndex = input.IndexOf("//");
		if (commentIndex != -1 && !Is.InsideStringLiteral(commentIndex, 2, input)) input = input[..commentIndex];


		if (input.At(0) != '@')
			input = $"@SO.Select({input}).First()";

		var selectorIndex = input.IndexOf('.');

		if (input.Length < 5 || selectorIndex < 0)
		{
			errorMsg = "Input must contain query body.";
			return (false, translated, selector, errorMsg);
		}

		selector = input[..selectorIndex];

		if (!DictionaryLiteral.ContainsKey(selector))
		{
			errorMsg = $"Invalid dictionary selector: {input[..selectorIndex]}";
			return (false, translated, selector, errorMsg);
		}

		input = ReplaceGenericMethods(input);

		input = input[(selectorIndex + 1)..];

		if (input.Contains('`'))
			input = ReplaceRegexLiteral(input);

		if (input.Contains("input"))
			input = await ReplaceInputAsync(input);

		if (input.Contains('!'))
			input = await ReplaceProcedureAsync(input);

		if (input.Contains('{'))
			input = ReplaceArrayInitializer(input);

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
			input = WideVariableRegex.Increment().Replace(input, m => 
			{
				if (Is.InsideStringLiteral(m.Index, m.Length, input))
					return m.Value;

				return WideVariable.GetIncrementString(m.Groups["name"].Value);
			});

		if (input.Contains("--"))
			input = WideVariableRegex.Decrement().Replace(input, m => 
			{
				if (Is.InsideStringLiteral(m.Index, m.Length, input))
					return m.Value;

				return WideVariable.GetDecrementString(m.Groups["name"].Value);
			});

		if (input.Contains('&'))
			input = WideVariableRegex.Reference().Replace(input, m => 
			{
				if (Is.InsideStringLiteral(m.Index, m.Length, input))
					return m.Value;

				return WideVariable.GetFormattedString(m.Groups["name"].Value); 
			});

		foreach (var (key, value) in ExtensionProperties)
			input = input.ReplaceFreeString(key, value);


		translated = input;

		return (true, translated, selector, errorMsg);
	}

	private static string ReplaceFreeChars(string input)
	{
		foreach (var (key, value) in EscapeCharacters)
			input = input.ReplaceFreeChar(key, value);
		return input;
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

	private static int _procedureID;
	private static async Task<string> ReplaceProcedureAsync(string input)
	{
		while (true)
		{
			var tmp = input.StringJoin();
			foreach (var procedure in ModuleManager.Procedures)
			{
				var procName = procedure.Name;

				var pattern = $@"{procedure.Name}\s*\!\s*\(";
				var match = Regex.Match(input, pattern);

				if (!match.Success || Is.InsideStringLiteral(match.Index, match.Length, input))
					continue;

				var paramStart = match.Index + match.Length;
				var paramEnd = Find.CloseBrace(input, '(', ')', paramStart - 1);
				var paramText = input[paramStart..paramEnd];
				var parameters = Split.ParameterTexts(paramText).Where(s => !string.IsNullOrEmpty(s));
				var evaluated = new List<object?>();
				foreach (var parameter in parameters)
					evaluated.Add(await QueryRunner.EvaluateExpressionAsync(parameter));
				var result = await procedure.RunAsync(evaluated);
				var varName = $"__proc_result_{_procedureID}_generated";
				++_procedureID;
				WideVariable.Variables[varName] = result;
				var sb = new StringBuilder(input);
				sb.Remove(match.Index, match.Length + paramEnd - paramStart + 1);
				sb.Insert(match.Index, $"&{varName}");
				input = sb.ToString();
			}
			if (tmp == input) 
				break;
		}
		return input;

	}

	private static async Task<string> ReplaceInputAsync(string input)
	{
		var builder = new StringBuilder(input);
		foreach (var match in InputRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			var value = await ManualQuery.GetInputStream();
			value = value.Replace("\"", "\\\"");
			builder.Insert(match.Index, $"\"{value}\"");
		}
		return builder.ToString();
	}

	private static string ReplaceGenericMethods(string input)
	{
		if (!input.Contains(".Cast<") && !input.Contains(".OfType<") && !input.Contains("static_cast<")) return input;

		var builder = new StringBuilder(input);

		foreach (var match in CastCallRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $".Cast(\"{match.Groups["type"].Value}\")");
		}

		foreach (var match in OfTypeCallRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $".OfType(\"{match.Groups["type"].Value}\")");
		}

		foreach (var match in StaticCastRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"{match.Groups["type"].Value}({match.Groups["operand"].Value})");
		}

		return builder.ToString();
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
		foreach (var match in RegexLiteralRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"\"{match.Groups["pattern"].Value}\".ToRegex()");
		}
		return builder.ToString();
	}
	private static string ReplaceWordLiteral(string input)
	{
		var builder = new StringBuilder(input);
		foreach (Match match in WordLiteralRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
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
		foreach (var match in DeduceLiteralRegex().Matches(input).Where(m => !Is.InsideStringLiteral(m.Index, m.Length, input)).Reverse())
		{
			builder.Remove(match.Index, match.Length);
			builder.Insert(match.Index, $"\"{match.Groups["name"].Value}\".Deduce()");
		}
		return builder.ToString();
	}

	[GeneratedRegex(@"input\s*\(\s*\)")]
	private static partial Regex InputRegex();

	[GeneratedRegex(@"`(?<pattern>.*?)`")]
	private static partial Regex RegexLiteralRegex();

	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*(?<type1>[A-Za-z])?(?<type2>[A-Za-z])?\s*/w")]
	private static partial Regex WordLiteralRegex();

	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*/d")]
	private static partial Regex DeduceLiteralRegex();

	[GeneratedRegex(@"\.Cast<(?<type>.*?)>\(\)")]
	private static partial Regex CastCallRegex();

	[GeneratedRegex(@"\.OfType<(?<type>.*?)>\(\)")]
	private static partial Regex OfTypeCallRegex();

	[GeneratedRegex(@"static_cast<(?<type>.*?)>\((?<operand>.*?)\)")]
	private static partial Regex StaticCastRegex();
}
