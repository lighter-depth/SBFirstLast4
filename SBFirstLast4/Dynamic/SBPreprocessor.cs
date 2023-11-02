using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using static SBFirstLast4.Dynamic.SelectorConstants;

namespace SBFirstLast4.Dynamic;

public static partial class SBPreprocessor
{
	public static bool IsInitialized { get; private set; } = false;
	public static async Task Initialize(HttpClient client)
	{
		var standard = await client.GetStringAsync($"sblibs/Standard.sbmdl");
		ImportModule(standard);
		IsInitialized = true;
	}

	private static readonly Dictionary<string, Macro> Macros = new();

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
		[".\\fc"] = $"{nameof(Extensions.DynamicExtensionHelper.FirstChar)}()",
		[".\\lc"] = $"{nameof(Extensions.DynamicExtensionHelper.LastChar)}()",
		[".\\c("] = $".{nameof(Word.Contains)}(",
		[".\\cl("] = $".{nameof(Word.CalcEffectiveDmg)}(",
		[".\\t2c("] = $".{nameof(WordTypeEx.TypeToChar)}(",
		[".\\t2s("] = $".{nameof(WordTypeEx.TypeToString)}(",
		[".Sort()"] = ".OrderBy(it)"
	};

	private static bool IsAuto = true;

	public static bool TryPreprocess(string input, [NotNullWhen(true)] out string[]? status, [NotNullWhen(false)] out string? errorMsg, string moduleName = "USER_DEFINED")
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
		if (symbol is not ("define" or "undef" or "show" or "clear" or "auto"))
		{
			errorMsg = "Couldn't recognize the specified directive type.";
			return false;
		}
		if (symbol is "show")
		{
			status = Macros
				.Select(x => $"Module: {x.Value.ModuleName}, "
				+ (x.Value is ObjectLikeMacro o
				? $"Key: {o.Name}, Value: {o.Body}"
				: x.Value is FunctionLikeMacro f
				? $"Sign: {f.Name}({string.Join(", ", f.Parameters)}), Body: {f.Body}"
				: "NULL"))
				.ToArray();
			return true;
		}
		if (symbol is "clear")
		{
			Macros.Clear();
			status = new[] { "Cleared up the Macro Dictionary." };
			return true;
		}
		var contents = input.Split(' ');

		if (symbol is "auto")
		{
			if (contents.Length != 2)
			{
				errorMsg = "Invalid syntax: #auto syntax must have one argument.";
				return false;
			}
			if (contents[1] == "enable")
			{
				IsAuto = true;
				status = new[] { "Auto processing enabled." };
				return true;
			}
			if (contents[1] == "disable")
			{
				IsAuto = false;
				status = new[] { "Auto processing disabled." };
				return true;
			}
			if (contents[1] == "toggle")
			{
				IsAuto = !IsAuto;
				status = new[] { $"Auto processing {(IsAuto ? "enabled" : "disabled")}." };
				return true;
			}
			errorMsg = "Invalid syntax: invalid argument for #auto directive.";
			return false;
		}
		if (symbol is "undef")
		{
			if (contents.Length != 2)
			{
				errorMsg = "Invalid syntax: #undef syntax must have one argument.";
				return false;
			}
			if (!Macros.Remove(contents[1]))
			{
				errorMsg = $"Specified macro {contents[1]} does not exist in dictionary.";
				return false;
			}
			status = new[] { $"Successfully removed macro {contents[1]} from the dictionary." };
			return true;
		}

		var match = FunctionLikeMacroRegex().Match(input);
		if (match.Success)
		{
			var functionLikeMacro = new FunctionLikeMacro
			{
				Name = match.Groups["name"].Value,
				Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
				Body = match.Groups["body"].Value,
				ModuleName = moduleName
			};
			Macros[functionLikeMacro.Name] = functionLikeMacro;
			status = new[] { $"Successfully added macro {match.Groups["name"]} to the dictionary." };
			return true;
		}

		if (contents.Length < 3)
		{
			errorMsg = "Invalid syntax: #define syntax must have two arguments.";
			return false;
		}
		var groups = DefineDirectiveRegex().Match(input).Groups;
		var objectLikeMacro = new ObjectLikeMacro
		{
			Name = groups["key"].Value,
			Body = groups["value"].Value,
			ModuleName = moduleName
		};
		Macros[objectLikeMacro.Name] = objectLikeMacro;

		status = new[] { $"Successfully added macro {contents[1]} to the dictionary." };
		return true;
	}

	private static string ExpandMacro(string input)
	{
		foreach (var macro in Macros.Values.Reverse())
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
				input = input.Replace(objectLikeMacro.Name, objectLikeMacro.Body);
		}
		return input;

	}

	public static bool TryProcess(string input, [NotNullWhen(true)] out string? translated, [NotNullWhen(true)] out string? selector, [NotNullWhen(false)] out string? errorMsg)
	{
		if (!IsAuto) return TryProcessManual(input, out translated, out selector, out errorMsg);

		(translated, selector, errorMsg) = (null, null, null);


		input = input.Trim();


		input = ExpandMacro(input);


		input = input.Replace("@item", "it");

		var commentIndex = input.IndexOf("//");
		if (commentIndex != -1 && !IsInsideStringLiteral(commentIndex, 2, input)) input = input[..commentIndex];


		if (input.At(0) != '@')
		{
			var lastbracketIndex = input.LastIndexOf('(');
			var isNotCollection = (lastbracketIndex >= 5 && input[(lastbracketIndex - 5)..lastbracketIndex] == "First")
							   || (lastbracketIndex >= 14 && input[(lastbracketIndex - 14)..lastbracketIndex] == "FirstOrDefault")
							   || (lastbracketIndex >= 4 && input[(lastbracketIndex - 4)..lastbracketIndex] == "Last")
							   || (lastbracketIndex >= 13 && input[(lastbracketIndex - 13)..lastbracketIndex] == "LastOrDefault")
							   || (lastbracketIndex >= 6 && input[(lastbracketIndex - 6)..lastbracketIndex] == "Single")
							   || (lastbracketIndex >= 15 && input[(lastbracketIndex - 15)..lastbracketIndex] == "SingleOrDefault")
							   || (lastbracketIndex >= 3 && input[(lastbracketIndex - 3)..lastbracketIndex] == "Sum")
							   || (lastbracketIndex >= 7 && input[(lastbracketIndex - 7)..lastbracketIndex] == "Average")
							   || (lastbracketIndex >= 3 && input[(lastbracketIndex - 3)..lastbracketIndex] == "All")
							   || (lastbracketIndex >= 3 && input[(lastbracketIndex - 3)..lastbracketIndex] == "Any")
							   || (lastbracketIndex >= 3 && input[(lastbracketIndex - 3)..lastbracketIndex] == "Min")
							   || (lastbracketIndex >= 3 && input[(lastbracketIndex - 3)..lastbracketIndex] == "Max")
							   || (lastbracketIndex >= 8 && input[(lastbracketIndex - 8)..lastbracketIndex] == "Contains")
							   || (lastbracketIndex >= 5 && input[(lastbracketIndex - 5)..lastbracketIndex] == "Count")
							   || (lastbracketIndex >= 9 && input[(lastbracketIndex - 9)..lastbracketIndex] == "LongCount");

			input = !input.Contains('[') || isNotCollection
				? $"@SO.Select(x => {input}).First()"
				: $"@SO.Except([0]).Cast(\"Object\").Union({input}.Cast(\"Object\"))";
		}

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

		input = ReplaceGenericMethods(input);

		if (input.Contains('['))
			input = ReplaceCollectionLiteral(input);

		if (input.Contains('`'))
			input = ReplaceRegexLiteral(input);

		if (input.Contains("/w"))
			input = ReplaceWordLiteral(input);

		if (input.Contains("/d"))
			input = ReplaceDeduceLiteral(input);

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


	private static bool TryProcessManual(string input, [NotNullWhen(true)] out string? translated, [NotNullWhen(true)] out string? selector, [NotNullWhen(false)] out string? errorMsg)
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
			errorMsg = $"Invalid dictionary selector: {input[..selectorIndex]}";
			return false;
		}

		translated = input[(selectorIndex + 1)..];

		return true;
	}

	private static string ReplaceGenericMethods(string input)
	{
		if (!input.Contains(".Cast<") && !input.Contains(".OfType<") && !input.Contains(".static_cast<")) return input;

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
			builder.Insert(match.Index, $".{match.Groups["type"].Value}({match.Groups["operand"].Value})");
		}

		return builder.ToString();
	}

	private static string ReplaceInterpolatedString(string input)
	{
		var count = 0;
		var result = InterpolationRegex().Replace(input, m => $"{{{count++}}}");
		return $"string.Format(\"{result}\", {string.Join(", ", Enumerable.Range(0, count).Select(i => $"{{{i}}}"))})";
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

	private static bool IsInsideStringLiteral(int startIndex, int length, string source)
	{
		if (startIndex < 0 || length < 0 || startIndex + length > source.Length)
			return false;

		int quoteCount = 0;
		for (int i = 0; i < startIndex; i++)
			if (source[i] == '"')
				quoteCount++;

		return quoteCount % 2 == 1;
	}

	public static void ImportModule(string moduleContent)
	{
		var match = ModuleRegex().Match(moduleContent);
		if (!match.Success) return;

		var name = match.Groups["name"].Value;
		var content = match.Groups["contents"].Value;
		var module = content.Split(Environment.NewLine);
		foreach (var execution in module)
			TryPreprocess(execution, out _, out _, name);
	}

	[GeneratedRegex(@"define\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	private static partial Regex DefineDirectiveRegex();

	[GeneratedRegex(@"define\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	private static partial Regex FunctionLikeMacroRegex();

	[GeneratedRegex(@"`(?<pattern>.*?)`")]
	private static partial Regex RegexLiteralRegex();


	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*(?<type1>[A-Za-z])?(?<type2>[A-Za-z])?\s*/w")]
	private static partial Regex WordLiteralRegex();

	[GeneratedRegex(@"/\s*(?<name>[ぁ-ゟー]+)\s*/d")]
	private static partial Regex DeduceLiteralRegex();

	[GeneratedRegex(@"(?!\[\]|new)\[(?<items>(?:[^[\]]|\[[^[\]]*\])*)\]")]
	private static partial Regex CollectionLiteralRegex();

	[GeneratedRegex(@"\.Cast<(?<type>.*?)>\(\)")]
	private static partial Regex CastCallRegex();

	[GeneratedRegex(@"\.OfType<(?<type>.*?)>\(\)")]
	private static partial Regex OfTypeCallRegex();

	[GeneratedRegex(@"\.static_cast<(?<type>.*?)>\((?<operand>.*?)\)")]
	private static partial Regex StaticCastRegex();
	[GeneratedRegex(@"{(\w+)}")]
	private static partial Regex InterpolationRegex();

	[GeneratedRegex(@"^[\s\n]*module\s+(?<name>\w+)\s*;(?<contents>[\s\S]*)$")]
	private static partial Regex ModuleRegex();


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
