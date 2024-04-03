using System.Text.RegularExpressions;
using BlazorDownloadFile;
using SBFirstLast4.Logging;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public sealed partial class QueryAgent
{
	public QueryContext CurrentContext => ContextStack.Peek();

	internal readonly Stack<QueryContext> ContextStack = [QueryContext.Script];

	private readonly Stack<ValueTuple> _moduleContextTrackingStack = [];

	public string StatementKind { get; private set; } = TextType.General;

	private readonly List<string> _statements = [];

	private readonly List<string> _newlineBuffer = [];

	private Procedure? _currentProcedure;

	private readonly List<string> _moduleBuffer = [];

	public async Task RunAsync(string input, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update, IBlazorDownloadFileService service)
	{
		var inputTrim = input.Trim();

		if (inputTrim.EndsWith('\\'))
		{
			_newlineBuffer.Add(inputTrim[..^1]);
			return;
		}

		inputTrim = _newlineBuffer.Append(inputTrim).StringJoin(string.Empty);
		_newlineBuffer.Clear();

		if(CurrentContext is QueryContext.Module)
		{
			ProcessModuleContext(inputTrim, output);
			return;
		}

		if (inputTrim.StartsWith('.') && QueryContexts.ScriptContextSpecifiers.Contains(inputTrim.Split().At(0)))
		{
			var context = inputTrim[1..];
			await SwitchContextAsync(context, output, setTranslated, handleDeletedFiles, update);
			return;
		}

		if (CurrentContext is QueryContext.Script)
			await RunScriptAsync(inputTrim, output, setTranslated, handleDeletedFiles, update, service);

		if (CurrentContext is QueryContext.AnonymousProcedure or QueryContext.NamedProcedure)
			_currentProcedure?.Push(inputTrim);
	}

	private async Task SwitchContextAsync(string contextStr, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update)
	{
		if (contextStr == "end" && ContextStack.Count > 1)
		{
			await endProcedureAsync();
			ContextStack.Pop();
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}
		if (contextStr == "default")
		{
			await endProcedureAsync();
			ContextStack.Clear();
			ContextStack.Push(QueryContext.Script);
			output.AddReflect($"Switched context to {CurrentContext}.");
		}
		if (contextStr == "script" && CurrentContext != QueryContext.Script)
		{
			await endProcedureAsync();
			ContextStack.Push(QueryContext.Script);
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}

		if(contextStr.Split().At(0) == "module" && CurrentContext != QueryContext.Module)
		{
			await endProcedureAsync();
			ContextStack.Push(QueryContext.Module);

			_moduleBuffer.Add($"module {contextStr.Split().At(1)};");
			output.AddReflect($"Switched context to {CurrentContext}.");
			return;
		}

		if (contextStr.Split().At(0) == "proc" && CurrentContext is not QueryContext.AnonymousProcedure and not QueryContext.NamedProcedure)
		{
			var procDeclaration = contextStr.Skip(4).SkipWhile(char.IsWhiteSpace).StringJoin();
			var procNameEnd = procDeclaration.IndexOf('!');
			var procName = procNameEnd == -1 ? null : procDeclaration.Take(..procNameEnd).StringJoin();
			var context = procName is null
						? QueryContext.AnonymousProcedure
						: QueryContext.NamedProcedure;

			var rparen = procDeclaration.LastIndexOf(')');
			var parameterText = rparen > procNameEnd + 1 ? procDeclaration.Take((procNameEnd + 2)..rparen).StringJoin() : string.Empty;
			var parameters = parameterText
							.Split(',')
							.Select(s => s.Trim())
							.Where(s => s.StartsWith('&') || s.StartsWith('^'))
							.ToList();

			_currentProcedure = new(procName ?? string.Empty, parameters, "USER_DEFINED", this, output, setTranslated, update, ManualQuery.Cancellation.Token);
			ContextStack.Push(context);
			output.AddReflect($"Switched context to {CurrentContext}.");
		}

		async Task endProcedureAsync()
		{
			if (CurrentContext == QueryContext.AnonymousProcedure && _currentProcedure is not null)
			{
				ContextStack.Push(QueryContext.RunningProcedure);
				await _currentProcedure.RunAsync();
				ContextStack.Pop();
			}

			if (CurrentContext == QueryContext.NamedProcedure && _currentProcedure is not null)
				ModuleManager.UserDefined.Procedures.Add(_currentProcedure.Clone());
		}
	}

	public async Task RunScriptAsync(string input, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update, IBlazorDownloadFileService service)
	{
		StatementKind = TextType.General;

		var inputTrim = input.Trim();

		if (inputTrim.StartsWith("#ephemeral") || inputTrim.StartsWith("#evaporate"))
		{
			Preprocessor.ProcessEphemeral(inputTrim, output);
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
			await Preprocessor.ProcessAsync(inputTrim, output, handleDeletedFiles, service);
			return;
		}

		inputTrim = Macro.Expand(inputTrim);

		inputTrim = ReplaceHash(inputTrim);

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
				await QueryRunner.RunStatementAsync(statement, output, setTranslated);
			}
			catch (Exception ex)
			{
				output.Add(ex.ToFormat(), TextType.Error);
			}

		_statements.Clear();
	}

	private void ProcessModuleContext(string input, Buffer output)
	{
		if (QueryContexts.ModuleContextSpecifiers.Contains(input.Split().At(0)))
		{
			if (input == ".end")
			{
				if (_moduleContextTrackingStack.Count < 1)
				{
					var module = Module.Compile(_moduleBuffer.StringJoin(Environment.NewLine), true);

					var success = ModuleManager.Import(module, out var status);

					output.Add(status, success ? TextType.Safe : TextType.Error);

					_moduleBuffer.Clear();
					ContextStack.Pop();
					return;
				}
				_moduleContextTrackingStack.Pop();
			}
			else if (input == ".default")
				_moduleContextTrackingStack.Clear();
			else
				_moduleContextTrackingStack.Push(default);
		}

		_moduleBuffer.Add(input);
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

	private static int HashId = 0;
	private static readonly List<string> HashDeclarations = [];
	private static string ReplaceHash(string input)
	{
		HashDeclarations.Clear();
		static string ReplaceHashCore(string input)
		{
			if (WideVariableRegex.HashStart().Match(input) is var match && match.Success && !Is.InsideStringLiteral(match.Index, match.Length, input))
			{
				var variableName = $"&__hash_{HashId}_generated";
				HashId++;
				var closeIndex = Find.CloseBrace(input, '{', '}', match.Index);
				var length = closeIndex - match.Index + 1;
				var sb = new StringBuilder(input);
				sb.Remove(match.Index, length);
				sb.Insert(match.Index, variableName);
				var hashContent = input[(match.Index + match.Length)..closeIndex];
				hashContent = ReplaceHashCore(hashContent);
				HashDeclarations.Add($"{variableName} = %{{{hashContent}}};");
				return sb.ToString();
			}
			return input;
		}
		var result = ReplaceHashCore(input);
		return $"{HashDeclarations.StringJoin()} {result}";
	}


	[GeneratedRegex("(?<=;)")]
	private static partial Regex SplitSemicolonRegex();

}

