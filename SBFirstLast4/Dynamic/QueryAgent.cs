using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SBFirstLast4.Logging;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public partial class QueryAgent
{
	public string? Context { get; private set; }

	private readonly List<string> _statements = new();

	private readonly List<string> _newlineBuffer = new();

	[MemberNotNull(nameof(Context))]
	public async Task RunAsync(string input, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		Context = string.Empty;

		var inputTrim = input.Trim();

		if (inputTrim.EndsWith('\\'))
		{
			_newlineBuffer.Add(inputTrim[..^1]);
			return;
		}

		inputTrim = _newlineBuffer.Append(inputTrim).StringJoin(string.Empty);

		_newlineBuffer.Clear();

		if(inputTrim.StartsWith("#ephemeral") || inputTrim.StartsWith("#evaporate"))
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
			catch(Exception ex) 
			{
				outputBuffer.Add(($"InternalException({ex.GetType().Name}): {ex.Message}", "Error"));
			}

		_statements.Clear();
	}


	private static void RunStatement(string input, Buffer outputBuffer, Action<string> setTranslated)
	{
		var inputTrim = input.Trim();

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

		if(WideVariableRegex.IncrementStatement().Match(inputTrim) is var increment && increment.Success)
		{
			WideVariable.Increment(increment.Groups["name"].Value);
			return;
		}

		if (WideVariableRegex.DecrementStatement().Match(inputTrim) is var decrement && decrement.Success)
		{
			WideVariable.Decrement(decrement.Groups["name"].Value);
			return;
		}

		foreach (var(regex, type) in WideVariableRegex.Assignments)
			if(regex.Match(inputTrim) is var assignMatch && assignMatch.Success)
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

		Context = "Monitor";

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
			outputBuffer.Add(($"Successfully deleted variable '{name}'.", string.Empty));
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

		var output = (dynamic?)ScriptExecutor.ExecuteDynamic(translated, selector);

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