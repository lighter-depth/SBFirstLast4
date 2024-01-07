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

	[MemberNotNull(nameof(Context))]
	public async Task RunAsync(string input, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		Context = string.Empty;

		var inputTrim = input.Trim();

		if(inputTrim.StartsWith("#ephemeral") || inputTrim.StartsWith("#evaporate"))
		{
			ProcessEphemeral(inputTrim, outputBuffer);
			return;
		}

		inputTrim = SBInterpreter.ExpandEphemeral(inputTrim);

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

		inputTrim = SBInterpreter.ExpandMacro(inputTrim);

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
				await RunStatementAsync(statement, outputBuffer, setTranslated, handleDeletedFiles);
			}
			catch(Exception ex) 
			{
				outputBuffer.Add(($"InternalException({ex.GetType().Name}): {ex.Message}", "Error"));
			}

		_statements.Clear();
	}


	private static async Task RunStatementAsync(string input, Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		var inputTrim = input.Trim();

		if (SBInterpreter.VariableDeclarationRegex().Match(inputTrim) is var varMatch && varMatch.Success)
		{
			await DefineVariableAsync(varMatch, outputBuffer, setTranslated);
			return;
		}

		if (SBInterpreter.DeleteVariableRegex().Match(inputTrim) is var deleteMatch && deleteMatch.Success)
		{
			DeleteVariable(deleteMatch, outputBuffer);
			return;
		}

		if (!SBInterpreter.TryInterpret(input, out var translated, out var selector, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBProcessException: {errorMsg}", "Error"));
			return;
		}

		setTranslated(translated);
		var output = await SBScriptExecutor.ExecuteAsync(translated, selector);
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
		if (!SBPreprocessor.TryProcessEphemerals(input, out var status, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBPreprocessException: {errorMsg}", "Error"));
			return;
		}

		outputBuffer.AddRange(status.Select(x => (x, string.Empty)));
	}

	private static async Task PreprocessAsync(string input, Buffer outputBuffer, Func<Task> handleDeletedFiles)
	{
		if (!SBPreprocessor.TryPreprocess(input, out var status, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBPreprocessException: {errorMsg}", "Error"));
			return;
		}

		outputBuffer.AddRange(status.Select(x => (x, string.Empty)));

		if (input.StartsWith("#delete"))
			await handleDeletedFiles();
	}

	private static async Task DefineVariableAsync(Match varMatch, Buffer outputBuffer, Action<string> setTranslated)
	{
		var name = varMatch.Groups["name"].Value;
		var expr = varMatch.Groups["expr"].Value;
		if (!SBInterpreter.TryInterpret(expr, out var translated, out var selector, out var errorMsg))
		{
			outputBuffer.Add(($"Error: SBProcessException: {errorMsg}", "Error"));
			return;
		}
		setTranslated(translated);
		var output = await SBScriptExecutor.ExecuteDynamicAsync(translated, selector);

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

	[GeneratedRegex("(?<=;)")]
	private static partial Regex SplitSemicolonRegex();


}