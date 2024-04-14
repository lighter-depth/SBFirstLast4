using Antlr4.Runtime;
using Microsoft.CSharp.RuntimeBinder;
using SBFirstLast4.Pages;
using System.Text.RegularExpressions;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public static partial class QueryRunner
{

	public static async Task<object?> EvaluateExpressionAsync(string expression)
	{
		expression = expression.Trim();

		if (expression.EndsWith(';'))
			expression = expression[..^1];

		if (expression == "\"\"")
			return string.Empty;

		var joined = expression.Split().StringJoin();

		if (joined == "input()")
			return await ManualQuery.GetInputStream();

		if (bool.TryParse(expression, out var @bool))
			return @bool;

		if (int.TryParse(expression, out var @int))
			return @int;

		if (joined == "{}")
			return Array.Empty<object>();

		if (WideVariableRegex.SingleReference().Match(expression) is var varMatch && varMatch.Success)
			return WideVariable.GetValue(varMatch.Groups["name"].Value);


		if (await ScriptExecutor.EvaluateSimpleExpressionAsync(expression) is { Success: true, Result: not null } simpleResult)
			return simpleResult.Result;

		if (await Interpreter.TryInterpretAsync(expression) is var interpret && !interpret.Success)
			return null;

		return ScriptExecutor.ExecuteDynamic(interpret.Translated, interpret.Selector);
	}

	public static async Task RunStatementAsync(string input, Buffer output, Action<string> setTranslated)
	{
		var inputTrim = input.Trim();

		if (inputTrim.EndsWith(';'))
			inputTrim = inputTrim[..^1];

		if (inputTrim.StartsWith("print"))
		{
			var lParen = inputTrim.IndexOf('(');

			if (lParen > 4 && inputTrim[..lParen].Trim() == "print")
			{
				var rParen = inputTrim.LastIndexOf(')');
				var expr = inputTrim[(lParen + 1)..rParen];
				var comma = expr.LastIndexOf(',');
				var color = TextType.General;
				if (comma >= 0 && !Is.InsideStringLiteral(comma, 1, expr) && !Is.InsideBrace(comma, 1, expr, '{', '}') && !Is.InsideBrace(comma, 1, expr, '(', ')'))
					(expr, color) = (expr[..comma], expr[(comma + 1)..]);

				var printValue = await EvaluateExpressionAsync(expr);
				output.Add(To.String(printValue), color);
				return;
			}
		}

		if (inputTrim.Split().At(0)?.StartsWith("throw") is true)
		{
			var expr = inputTrim.Take(5..).StringJoin();
			var exception = await EvaluateExpressionAsync(expr);
			throw (Exception)exception!;
		}

		if (RecordRegex.Statement().Match(inputTrim) is var recordMatch && recordMatch.Success)
		{
			Record.Emit(recordMatch.Groups["name"].Value, recordMatch.Groups["expr"].Value);
			return;
		}

		if (RecordRegex.Enum().Match(inputTrim) is { Success: true } enumMatch)
		{
			DefineEnum(inputTrim, enumMatch);
			return;
		}

		if (WideVariableRegex.Hash().Match(inputTrim) is { Success: true } hashMatch)
		{
			await DefineHashAsync(hashMatch);
			return;
		}

		if (WideVariableRegex.Declaration().Match(inputTrim) is { Success: true } varMatch)
		{
			await DefineVariableAsync(varMatch);
			return;
		}

		if (WideVariableRegex.Definition().Match(inputTrim) is { Success: true } varMatchAttr)
		{
			await DefineAttributedVariableAsync(varMatchAttr);
			return;
		}

		if (ProcedureDeletionRegex().Match(inputTrim) is { Success: true } procDeleteMatch)
		{
			DeleteProcedure(procDeleteMatch, output);
			return;
		}

		if (WideVariableRegex.Deletion().Match(inputTrim) is { Success: true } deleteMatch)
		{
			DeleteVariable(deleteMatch, output);
			return;
		}

		if (WideVariableRegex.MemberAssign().IsMatch(inputTrim) && Find.FirstFree(inputTrim, '=') > 0)
		{
			await AssignMember(inputTrim);
			return;
		}

		if (WideVariableRegex.IncrementStatement().Match(inputTrim) is { Success: true } increment)
		{
			WideVariable.Increment(increment.Groups["name"].Value);
			return;
		}

		if (WideVariableRegex.DecrementStatement().Match(inputTrim) is { Success: true } decrement)
		{
			WideVariable.Decrement(decrement.Groups["name"].Value);
			return;
		}

		foreach (var (regex, type) in WideVariableRegex.Assignments)
			if (regex.Match(inputTrim) is { Success: true } assignMatch)
			{
				await AssignVariable(assignMatch, output, setTranslated, type);
				return;
			}

		if (await Interpreter.TryInterpretAsync(inputTrim) is var interpret && !interpret.Success)
		{
			output.Add($"Error: SBProcessException: {interpret.ErrorMsg}", TextType.Error);
			return;
		}

		setTranslated(interpret.Translated ?? string.Empty);
		var result = ScriptExecutor.Execute(interpret.Translated, interpret.Selector);
		output.Add(result, result.Contains("Error:") ? TextType.Error : TextType.General);

		output.AddReflect(interpret.Translated ?? string.Empty);
	}

	private static void DefineEnum(string input, Match match)
	{
		var enumName = match.Groups["name"].Value;
		var lbrace = input.IndexOf('{') + 1;
		var rbrace = input.LastIndexOf('}');
		var enumMembers = input[lbrace..rbrace].Split(",").Select(s => s.Trim()).ToArray();
		Record.EmitEnum(enumName, enumMembers);
	}

	private static async Task DefineHashAsync(Match hashMatch)
	{
		var name = hashMatch.Groups["name"].Value;
		var expr = hashMatch.Groups["expr"].Value;

		var isCollection = false;

		var matchSample = WideVariableRegex.HashInitializer().Match(expr);
		if (matchSample.Groups["value"].Value.StartsWith('{'))
			isCollection = true;


		var matches = (isCollection ? WideVariableRegex.HashArrayInitializer() : WideVariableRegex.HashInitializer()).Matches(expr).Cast<Match>().ToArray();

		if (matches.Length == 0)
		{
			var objHash = typeof(Dictionary<,>).MakeGenericType(typeof(object), typeof(object));
			WideVariable.SetValue(name, Activator.CreateInstance(objHash));
			return;
		}
		var sample = matches[0];

		var collectionEnd = isCollection ? "}" : string.Empty;

		var keySample = await EvaluateExpressionAsync(sample.Groups["key"].Value) ?? new object();
		var valueSample = await EvaluateExpressionAsync(sample.Groups["value"].Value + collectionEnd) ?? new object();

		var hashType = typeof(Dictionary<,>).MakeGenericType(keySample.GetType(), valueSample.GetType());

		var hashBase = Activator.CreateInstance(hashType);

		var add = hashType.GetMethod("Add");

		foreach (var match in matches)
		{
			var key = await EvaluateExpressionAsync(match.Groups["key"].Value);
			var value = await EvaluateExpressionAsync(match.Groups["value"].Value + collectionEnd);

			add?.Invoke(hashBase, [key, value]);
		}

		WideVariable.SetValue(name, hashBase);
	}

	private static async Task DefineVariableAsync(Match match)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;

		WideVariable.SetValue(name, await EvaluateExpressionAsync(expr));
	}

	private static async Task DefineAttributedVariableAsync(Match match)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;

		var attribute = match.Groups["attributes"].Value;

		var isReadonly = attribute is "let" or "const";
		var isAssignable = attribute is "var" or "let";

		WideVariable.DefineValue(name, await EvaluateExpressionAsync(expr), isReadonly, isAssignable);
	}

	private static void DeleteProcedure(Match match, Buffer output)
	{
		var name = match.Groups["name"].Value;

		var deleteIndex = ModuleManager.UserDefined.Procedures.FindIndex(p => p.Name == name);

		if (deleteIndex == -1)
		{
			output.Add($"Specified procedure '{name}!' does not exist in USER_DEFINED module.", TextType.Error);
			return;
		}

		ModuleManager.UserDefined.Procedures.RemoveAt(deleteIndex);
		output.AddReflect($"Successfully deleted procedure '{name}!' from USER_DEFINED module.");
	}
	private static void DeleteVariable(Match match, Buffer output)
	{
		var name = match.Groups["name"].Value;

		if (!WideVariable.RemoveValue(name))
		{
			output.Add($"Specified variable '{name}' does not exist.", TextType.Error);
			return;
		}

		output.AddReflect($"Successfully deleted variable '{name}'.");
	}

	private static async Task AssignMember(string input)
	{
		var stream = new AntlrInputStream(input);

		var lexer = new SBProcLangLexer(stream);
		var tokens = new CommonTokenStream(lexer);
		var parser = new SBProcLangParser(tokens)
		{
			ErrorHandler = new DefaultErrorStrategy()
		};

		var listener_lexer = new ErrorListener<int>();
		var listener_parser = new ErrorListener<IToken>();

		lexer.RemoveErrorListeners();
		parser.RemoveErrorListeners();
		lexer.AddErrorListener(listener_lexer);
		parser.AddErrorListener(listener_parser);

		var tree = parser.member_assign_expr();

		if (listener_lexer.HadError || listener_parser.HadError)
			return;

		try
		{
			var visitor = new SBProcLangVisitor();
			await visitor.Visit(tree);
		}
		catch (Exception e)
		when (e is NullReferenceException or SBProcLangVisitorException or RuntimeBinderException)
		{
		}
	}

	private static async Task AssignVariable(Match match, Buffer output, Action<string> setTranslated, AssignmentType assignmentType)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;
		if (await Interpreter.TryInterpretAsync(expr) is var interpret && !interpret.Success)
		{
			output.Add($"Error: SBProcessException: {interpret.ErrorMsg}", TextType.Error);
			return;
		}
		setTranslated(interpret.Translated ?? string.Empty);

		var result = ScriptExecutor.ExecuteDynamic(interpret.Translated, interpret.Selector) as dynamic;

		switch (assignmentType)
		{
			case AssignmentType.Add:
				WideVariable.SetValue(name, WideVariable.GetValue(name) + result);
				break;

			case AssignmentType.Subtract:
				WideVariable.SetValue(name, WideVariable.GetValue(name) - result);
				break;

			case AssignmentType.Multiply:
				WideVariable.SetValue(name, WideVariable.GetValue(name) * result);
				break;

			case AssignmentType.Divide:
				WideVariable.SetValue(name, WideVariable.GetValue(name) / result);
				break;

			case AssignmentType.Modulus:
				WideVariable.SetValue(name, WideVariable.GetValue(name) % result);
				break;

			case AssignmentType.And:
				WideVariable.SetValue(name, WideVariable.GetValue(name) & result);
				break;

			case AssignmentType.Or:
				WideVariable.SetValue(name, WideVariable.GetValue(name) | result);
				break;

			case AssignmentType.Xor:
				WideVariable.SetValue(name, WideVariable.GetValue(name) ^ result);
				break;

			case AssignmentType.LeftShift:
				WideVariable.SetValue(name, WideVariable.GetValue(name) << result);
				break;

			case AssignmentType.RightShift:
				WideVariable.SetValue(name, WideVariable.GetValue(name) >> result);
				break;

			case AssignmentType.Coarse:
				WideVariable.SetValue(name, WideVariable.GetValue(name) ?? result);
				break;

			default:
				break;
		}

		output.AddReflect(interpret.Translated ?? string.Empty);
	}

	[GeneratedRegex(@"^\s*delete\s*(?<name>[^!]*)\s*!\s*$")]
	private static partial Regex ProcedureDeletionRegex();
}
