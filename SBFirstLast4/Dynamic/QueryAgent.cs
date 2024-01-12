using System.Text;
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

	public string StatementKind { get; private set; } = string.Empty;

	private readonly List<string> _statements = new();

	private readonly List<string> _newlineBuffer = new();

	private Procedure? _currentProcedure;

	public static object? EvaluateExpression(string expression)
	{
		if (!Interpreter.TryInterpret(expression, out var translated, out var selector, out _))
			return null;

		return ScriptExecutor.ExecuteDynamic(translated, selector);
	}

	public async Task RunAsync(string input, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
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
			SwitchContext(context, outputBuffer, setTranslated, handleDeletedFiles);
			return;
		}

		if (CurrentContext == QueryContext.Script)
			await RunScriptAsync(inputTrim, outputBuffer, setTranslated, handleDeletedFiles);

		if (CurrentContext == QueryContext.Procedure)
			_currentProcedure?.Push(inputTrim);
	}

	private async void SwitchContext(string contextStr, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		if (contextStr == "end" && ContextStack.Count > 1)
		{
			if (CurrentContext == QueryContext.Procedure && _currentProcedure is not null)
				await _currentProcedure.FlushAsync(outputBuffer, setTranslated, handleDeletedFiles);
			ContextStack.Pop();
			notify(outputBuffer, $"Switched context to {CurrentContext}.");
			return;
		}
		if (contextStr == "default")
		{
			ContextStack.Clear();
			ContextStack.Push(QueryContext.Script);
			notify(outputBuffer, $"Switched context to {CurrentContext}.");
		}
		if (contextStr == "script" && CurrentContext != QueryContext.Script)
		{
			ContextStack.Push(QueryContext.Script);
			notify(outputBuffer, $"Switched context to {CurrentContext}.");
			return;
		}
		if (contextStr is "proc" or "procedure" && CurrentContext != QueryContext.Procedure)
		{
			_currentProcedure = new();
			ContextStack.Push(QueryContext.Procedure);
			notify(outputBuffer, $"Switched context to {CurrentContext}.");
			return;
		}

		static void notify(Buffer buffer, string str)
		{
			if (ManualQuery.IsReflect)
				buffer.Add((str, "Monitor"));
		}
	}

	public async Task RunScriptAsync(string input, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		StatementKind = string.Empty;

		var inputTrim = input.Trim();

		if (inputTrim.StartsWith("#ephemeral") || inputTrim.StartsWith("#evaporate"))
		{
			ProcessEphemeral(inputTrim, outputBuffer);
			return;
		}

		inputTrim = Interpreter.ExpandEphemeral(inputTrim);

		if (inputTrim.StartsWith("#pragma monitor"))
		{
			await PragmaMonitorAsync(inputTrim, outputBuffer);
			return;
		}

		if (inputTrim == "#clear")
		{
			outputBuffer.Clear();
			return;
		}

		if (inputTrim.At(0) == '#')
		{
			await PreprocessAsync(inputTrim, outputBuffer, handleDeletedFiles);
			return;
		}

		inputTrim = Interpreter.ExpandMacro(inputTrim);

		var hashMatches = WideVariableRegex.HashExpression().Matches(inputTrim).Cast<Match>().ToArray();
		var hashVariableIndex = 0;
		if (hashMatches.Length > 0)
			foreach(var (match, i) in hashMatches.WithIndex())
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
				RunStatement(statement, outputBuffer, setTranslated);
			}
			catch (Exception ex)
			{
				outputBuffer.Add(($"InternalException({ex.GetType().Name}): {ex.Message}", "Error"));
			}

		_statements.Clear();
	}

	private static void RunStatement(string input, Buffer outputBuffer, Action<string> setTranslated)
	{
		var inputTrim = input.Trim();

		if (RecordRegex.Statement().Match(inputTrim) is var recordMatch && recordMatch.Success)
		{
			Record.Emit(recordMatch.Groups["name"].Value, recordMatch.Groups["expr"].Value);
			return;
		}

		if (WideVariableRegex.Hash().Match(inputTrim) is var hashMatch && hashMatch.Success)
		{
			DefineHash(hashMatch);
			return;
		}

		if (WideVariableRegex.Declaration().Match(inputTrim) is var varMatch && varMatch.Success)
		{
			DefineVariable(varMatch, outputBuffer, setTranslated);
			return;
		}

		if (WideVariableRegex.Deletion().Match(inputTrim) is var deleteMatch && deleteMatch.Success)
		{
			DeleteVariable(deleteMatch, outputBuffer);
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
				AssignVariable(assignMatch, outputBuffer, setTranslated, type);
				return;
			}

		if (!Interpreter.TryInterpret(input, out var translated, out var selector, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBProcessException: {errorMsg}", "Error"));
			return;
		}

		setTranslated(translated);
		var output = ScriptExecutor.Execute(translated, selector);
		outputBuffer.Add((output, output.Contains("Error:") ? "Error" : string.Empty));

		if (ManualQuery.IsReflect)
			outputBuffer.Add((translated, "Monitor"));
	}

	private async Task PragmaMonitorAsync(string input, Buffer outputBuffer)
	{
		var info = await Server.GetAsync();
		string[] infoSplit;

		StatementKind = "Monitor";

		if (input == "#pragma monitor $ALL")
		{
			infoSplit = info.Split("00\"},");
			outputBuffer.AddRange(infoSplit.Select((x, i) => (x + (i == infoSplit.Length - 1 ? string.Empty : "00\"}"), (i & 1) == 1 ? string.Empty : "Monitor")));
			outputBuffer.Add((infoSplit.Length.ToString(), "Safe"));
			return;
		}

		infoSplit = info.Split("00\"},").TakeLast(10).ToArray();
		outputBuffer.AddRange(infoSplit.Select((x, i) => (x + (i == infoSplit.Length - 1 ? string.Empty : "00\"}"), (i & 1) == 1 ? string.Empty : "Monitor")));
		outputBuffer.Add((info.Split("00\"},").Length.ToString(), "Safe"));
	}

	private static void ProcessEphemeral(string input, Buffer outputBuffer)
	{
		if (!Preprocessor.TryProcessEphemerals(input, out var status, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBPreprocessException: {errorMsg}", "Error"));
			return;
		}

		outputBuffer.AddRange(status.Select(x => (x, string.Empty)));
	}

	private static async Task PreprocessAsync(string input, Buffer outputBuffer, Func<Task> handleDeletedFiles)
	{
		if (!Preprocessor.TryPreprocess(input, out var status, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBPreprocessException: {errorMsg}", "Error"));
			return;
		}

		outputBuffer.AddRange(status.Select(x => (x, string.Empty)));

		if (input.StartsWith("#delete"))
			await handleDeletedFiles();
	}

	private static void DefineHash(Match hashMatch)
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

		var keySample = EvaluateExpression(sample.Groups["key"].Value) ?? new object();
		var valueSample = EvaluateExpression(sample.Groups["value"].Value + collectionEnd) ?? new object();

		var hashType = typeof(Dictionary<,>).MakeGenericType(keySample.GetType(), valueSample.GetType());

		var hashBase = Activator.CreateInstance(hashType);

		var add = hashType.GetMethod("Add");

		foreach (var match in matches)
		{
			var key = EvaluateExpression(match.Groups["key"].Value);
			var value = EvaluateExpression(match.Groups["value"].Value + collectionEnd);

			add?.Invoke(hashBase, new[] { key, value });
		}

		WideVariable.Variables[name] = hashBase;
	}

	private static void DefineVariable(Match match, Buffer outputBuffer, Action<string> setTranslated)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;
		if (!Interpreter.TryInterpret(expr, out var translated, out var selector, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBProcessException: {errorMsg}", "Error"));
			return;
		}
		setTranslated(translated);
		var output = ScriptExecutor.ExecuteDynamic(translated, selector);

		WideVariable.Variables[name] = output;

		if (ManualQuery.IsReflect)
			outputBuffer.Add((translated, "Monitor"));
	}

	private static void DeleteVariable(Match match, Buffer outputBuffer)
	{
		var name = match.Groups["name"].Value;

		if (!WideVariable.Variables.Remove(name))
		{
			outputBuffer.Add(($"Specified variable '{name}' does not exist.", "Error"));
			return;
		}

		if (ManualQuery.IsReflect)
			outputBuffer.Add(($"Successfully deleted variable '{name}'.", "Monitor"));
	}

	private static void AssignVariable(Match match, Buffer outputBuffer, Action<string> setTranslated, AssignmentType assignmentType)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;
		if (!Interpreter.TryInterpret(expr, out var translated, out var selector, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBProcessException: {errorMsg}", "Error"));
			return;
		}
		setTranslated(translated);

		var output = ScriptExecutor.ExecuteDynamic(translated, selector) as dynamic;

		switch (assignmentType)
		{
			case AssignmentType.Add:
				WideVariable.Variables[name] += output;
				break;

			case AssignmentType.Subtract:
				WideVariable.Variables[name] -= output;
				break;

			case AssignmentType.Multiply:
				WideVariable.Variables[name] *= output;
				break;

			case AssignmentType.Divide:
				WideVariable.Variables[name] /= output;
				break;

			case AssignmentType.Modulus:
				WideVariable.Variables[name] %= output;
				break;

			case AssignmentType.And:
				WideVariable.Variables[name] &= output;
				break;

			case AssignmentType.Or:
				WideVariable.Variables[name] |= output;
				break;

			case AssignmentType.Xor:
				WideVariable.Variables[name] ^= output;
				break;

			case AssignmentType.LeftShift:
				WideVariable.Variables[name] <<= output;
				break;

			case AssignmentType.RightShift:
				WideVariable.Variables[name] >>= output;
				break;

			case AssignmentType.Coarse:
				WideVariable.Variables[name] ??= output;
				break;

			default:
				break;
		}

		if (ManualQuery.IsReflect)
			outputBuffer.Add((translated, "Monitor"));
	}

	[GeneratedRegex("(?<=;)")]
	private static partial Regex SplitSemicolonRegex();

}

public enum QueryContext
{
	Script, Module, Procedure
}