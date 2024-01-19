using System.Diagnostics;
using System.Text.RegularExpressions;
using SBFirstLast4.Logging;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public partial class QueryAgent
{
	public QueryContext CurrentContext => ContextStack.Peek();

	private readonly Stack<QueryContext> ContextStack = ((Func<Stack<QueryContext>>)(() =>
	{
		var stack = new Stack<QueryContext>();
		stack.Push(QueryContext.Script);
		return stack;
	}))();

	public string StatementKind { get; private set; } = TextType.General;

	private readonly List<string> _statements = new();

	private readonly List<string> _newlineBuffer = new();

	private Procedure _currentProcedure = new();

	public static async Task<object?> EvaluateExpression(string expression)
	{
		expression = expression.Trim();

		if (expression.EndsWith(';'))
			expression = expression[..^1];

		if (expression.Split().StringJoin() == "input()")
			return await ManualQuery.GetInputStream();

		if (int.TryParse(expression, out var @int))
			return @int;

		if (WideVariableRegex.SingleRegerence().Match(expression) is var varMatch && varMatch.Success)
			return WideVariable.Variables[varMatch.Groups["name"].Value];

		if (await Interpreter.TryInterpretAsync(expression) is var interpret && !interpret.Result)
			return null;

		return ScriptExecutor.ExecuteDynamic(interpret.Translated, interpret.Selector);
	}

	public async Task RunAsync(string input, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update)
	{
		var inputTrim = input.Trim();

		if (inputTrim.EndsWith('\\'))
		{
			_newlineBuffer.Add(inputTrim[..^1]);
			return;
		}

		inputTrim = _newlineBuffer.Append(inputTrim).StringJoin(string.Empty);
		_newlineBuffer.Clear();

		if (inputTrim.StartsWith('.'))
		{
			var context = inputTrim[1..];
			await SwitchContextAsync(context, output, setTranslated, handleDeletedFiles, update);
			return;
		}

		if (CurrentContext == QueryContext.Script)
			await RunScriptAsync(inputTrim, output, setTranslated, handleDeletedFiles);

		if (CurrentContext == QueryContext.Procedure)
			_currentProcedure?.Push(inputTrim);
	}

	private async Task SwitchContextAsync(string contextStr, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update)
	{
		if (contextStr == "end" && ContextStack.Count > 1)
		{
			if (CurrentContext == QueryContext.Procedure)
			{
				ContextStack.Push(QueryContext.RunningProcedure);
				await _currentProcedure.FlushAsync(output, setTranslated, update);
				ContextStack.Pop();
			}

			ContextStack.Pop();
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}
		if (contextStr == "default")
		{
			ContextStack.Clear();
			ContextStack.Push(QueryContext.Script);
			output.AddReflect($"Switched context to {CurrentContext}.");
		}
		if (contextStr == "script" && CurrentContext != QueryContext.Script)
		{
			ContextStack.Push(QueryContext.Script);
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}
		if (contextStr is "proc" or "procedure" && CurrentContext != QueryContext.Procedure)
		{
			_currentProcedure.Clear();
			ContextStack.Push(QueryContext.Procedure);
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}
	}

	public async Task RunScriptAsync(string input, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		StatementKind = TextType.General;

		var inputTrim = input.Trim();

		if (inputTrim.StartsWith("#ephemeral") || inputTrim.StartsWith("#evaporate"))
		{
			ProcessEphemeral(inputTrim, output);
			return;
		}

		inputTrim = Macro.ExpandEphemeral(inputTrim);

		if (inputTrim.StartsWith("#pragma monitor"))
		{
			await PragmaMonitorAsync(inputTrim, output);
			return;
		}

		if (inputTrim == "#clear")
		{
			output.Clear();
			return;
		}

		if (inputTrim.At(0) == '#')
		{
			await PreprocessAsync(inputTrim, output, handleDeletedFiles);
			return;
		}

		inputTrim = Macro.Expand(inputTrim);

		var hashMatches = WideVariableRegex.HashExpression().Matches(inputTrim).Cast<Match>().ToArray();
		var hashVariableIndex = 0;
		if (hashMatches.Length > 0)
			foreach (var (match, i) in hashMatches.WithIndex())
			{
				var variableName = $"&__hash_{hashVariableIndex++}_generated";
				var length = match.Value.At(^1) is '}' or ' ' ? match.Length : match.Length - 1;
				var sb = new StringBuilder(inputTrim);
				sb.Remove(match.Index, length);
				sb.Insert(match.Index, variableName);
				inputTrim = $"{variableName} = %{{{match.Groups["expr"]}}}; {sb}; delete {variableName}";
			}

		var statements = SplitSemicolonRegex()
						.Split(inputTrim)
						.Where(s => !string.IsNullOrWhiteSpace(s))
						.ToArray();

		if (statements.Length < 1)
			return;

		if (statements[^1].TrimEnd().EndsWith(';'))
		{
			_statements.AddRange(statements.Select(s => s[..^1]));
			return;
		}

		var formattedStatements = statements.Select(s => s.TrimEnd().EndsWith(';') ? s[..^1] : s);


		foreach (var statement in _statements.Concat(formattedStatements))
			try
			{
				await RunStatementAsync(statement, output, setTranslated);
			}
			catch (Exception ex)
			{
				output.Add($"InternalException({ex.GetType().Name}): {ex.Message}", TextType.Error);
			}

		_statements.Clear();
	}

	public static async Task RunStatementAsync(string input, Buffer output, Action<string> setTranslated)
	{
		var inputTrim = input.Trim();

		if (inputTrim.EndsWith(';'))
			inputTrim = inputTrim[..^1];

		if (inputTrim.StartsWith("print"))
		{
			var lParen = inputTrim.IndexOf('(');
			var rParen = inputTrim.LastIndexOf(')');
			var printValue = await EvaluateExpression(inputTrim[(lParen + 1)..rParen]);
			output.Add(To.String(printValue), TextType.General);
			return;
		}

		if (RecordRegex.Statement().Match(inputTrim) is var recordMatch && recordMatch.Success)
		{
			Record.Emit(recordMatch.Groups["name"].Value, recordMatch.Groups["expr"].Value);
			return;
		}

		if (WideVariableRegex.Hash().Match(inputTrim) is var hashMatch && hashMatch.Success)
		{
			await DefineHashAsync(hashMatch);
			return;
		}

		if (WideVariableRegex.Declaration().Match(inputTrim) is var varMatch && varMatch.Success)
		{
			await DefineVariableAsync(varMatch, output, setTranslated);
			return;
		}

		if (WideVariableRegex.Deletion().Match(inputTrim) is var deleteMatch && deleteMatch.Success)
		{
			DeleteVariable(deleteMatch, output);
			return;
		}

		if (WideVariableRegex.IncrementStatement().Match(inputTrim) is var increment && increment.Success)
		{
			WideVariable.Increment(increment.Groups["name"].Value);
			return;
		}

		if (WideVariableRegex.DecrementStatement().Match(inputTrim) is var decrement && decrement.Success)
		{
			WideVariable.Decrement(decrement.Groups["name"].Value);
			return;
		}

		foreach (var (regex, type) in WideVariableRegex.Assignments)
			if (regex.Match(inputTrim) is var assignMatch && assignMatch.Success)
			{
				await AssignVariable(assignMatch, output, setTranslated, type);
				return;
			}

		if (await Interpreter.TryInterpretAsync(input) is var interpret && !interpret.Result)
		{
			output.Add($"Error: SBProcessException: {interpret.ErrorMsg}", TextType.Error);
			return;
		}

		setTranslated(interpret.Translated ?? string.Empty);
		var result = ScriptExecutor.Execute(interpret.Translated, interpret.Selector);
		output.Add(result, result.Contains("Error:") ? TextType.Error : TextType.General);

		output.AddReflect(interpret.Translated ?? string.Empty);
	}

	private async Task PragmaMonitorAsync(string input, Buffer output)
	{
		var info = await Server.GetAsync();
		string[] infoSplit;

		StatementKind = TextType.Monitor;

		if (input == "#pragma monitor $ALL")
		{
			infoSplit = info.Split("00\"},");
			output.AddRange(infoSplit.Select((x, i) => (x + (i == infoSplit.Length - 1 ? string.Empty : "00\"}"), (i & 1) == 1 ? TextType.General : TextType.Monitor)));
			output.Add(infoSplit.Length.ToString(), TextType.Safe);
			return;
		}

		infoSplit = info.Split("00\"},").TakeLast(10).ToArray();
		output.AddRange(infoSplit.Select((x, i) => (x + (i == infoSplit.Length - 1 ? string.Empty : "00\"}"), (i & 1) == 1 ? TextType.General : TextType.Monitor)));
		output.Add(info.Split("00\"},").Length.ToString(), TextType.Safe);
	}

	private static void ProcessEphemeral(string input, Buffer output)
	{
		if (!Preprocessor.TryProcessEphemerals(input, out var status, out var errorMsg))
		{
			output.Add($"Error: SBPreprocessException: {errorMsg}", TextType.Error);
			return;
		}

		output.AddRange(status.Select(x => (x, string.Empty)));
	}

	private static async Task PreprocessAsync(string input, Buffer output, Func<Task> handleDeletedFiles)
	{
		if (!Preprocessor.TryProcess(input, out var status, out var errorMsg))
		{
			output.Add(($"Error: SBPreprocessException: {errorMsg}", "Error"));
			return;
		}

		output.AddRange(status.Select(x => (x, string.Empty)));

		if (input.StartsWith("#delete"))
			await handleDeletedFiles();
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
			WideVariable.Variables[name] = Activator.CreateInstance(objHash);
			return;
		}
		var sample = matches[0];

		var collectionEnd = isCollection ? "}" : string.Empty;

		var keySample = await EvaluateExpression(sample.Groups["key"].Value) ?? new object();
		var valueSample = await EvaluateExpression(sample.Groups["value"].Value + collectionEnd) ?? new object();

		var hashType = typeof(Dictionary<,>).MakeGenericType(keySample.GetType(), valueSample.GetType());

		var hashBase = Activator.CreateInstance(hashType);

		var add = hashType.GetMethod("Add");

		foreach (var match in matches)
		{
			var key = await EvaluateExpression(match.Groups["key"].Value);
			var value = await EvaluateExpression(match.Groups["value"].Value + collectionEnd);

			add?.Invoke(hashBase, new[] { key, value });
		}

		WideVariable.Variables[name] = hashBase;
	}

	private static async Task DefineVariableAsync(Match match, Buffer output, Action<string> setTranslated)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;
		/*
		if (!Interpreter.TryInterpret(expr, out var translated, out var selector, out var errorMsg))
		{
			output.Add($"Error: SBProcessException: {errorMsg}", TextType.Error);
			return;
		}
		setTranslated(translated);
		var result = ScriptExecutor.ExecuteDynamic(translated, selector);
		

		WideVariable.Variables[name] = result;

		output.AddReflect(translated);
		*/

		WideVariable.Variables[name] = await EvaluateExpression(expr);
	}

	private static void DeleteVariable(Match match, Buffer output)
	{
		var name = match.Groups["name"].Value;

		if (!WideVariable.Variables.Remove(name))
		{
			output.Add($"Specified variable '{name}' does not exist.", TextType.Error);
			return;
		}

		output.AddReflect($"Successfully deleted variable '{name}'.");
	}

	private static async Task AssignVariable(Match match, Buffer output, Action<string> setTranslated, AssignmentType assignmentType)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;
		if (await Interpreter.TryInterpretAsync(expr) is var interpret && !interpret.Result)
		{
			output.Add($"Error: SBProcessException: {interpret.ErrorMsg}", TextType.Error);
			return;
		}
		setTranslated(interpret.Translated ?? string.Empty);

		var result = ScriptExecutor.ExecuteDynamic(interpret.Translated, interpret.Selector) as dynamic;

		switch (assignmentType)
		{
			case AssignmentType.Add:
				WideVariable.Variables[name] += result;
				break;

			case AssignmentType.Subtract:
				WideVariable.Variables[name] -= result;
				break;

			case AssignmentType.Multiply:
				WideVariable.Variables[name] *= result;
				break;

			case AssignmentType.Divide:
				WideVariable.Variables[name] /= result;
				break;

			case AssignmentType.Modulus:
				WideVariable.Variables[name] %= result;
				break;

			case AssignmentType.And:
				WideVariable.Variables[name] &= result;
				break;

			case AssignmentType.Or:
				WideVariable.Variables[name] |= result;
				break;

			case AssignmentType.Xor:
				WideVariable.Variables[name] ^= result;
				break;

			case AssignmentType.LeftShift:
				WideVariable.Variables[name] <<= result;
				break;

			case AssignmentType.RightShift:
				WideVariable.Variables[name] >>= result;
				break;

			case AssignmentType.Coarse:
				WideVariable.Variables[name] ??= result;
				break;

			default:
				break;
		}

		output.AddReflect(interpret.Translated ?? string.Empty);
	}

	[GeneratedRegex("(?<=;)")]
	private static partial Regex SplitSemicolonRegex();

}

public enum QueryContext
{
	Script, Module, Procedure, RunningProcedure
}