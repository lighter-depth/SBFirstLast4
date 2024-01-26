using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Microsoft.CSharp.RuntimeBinder;
using SBFirstLast4.Logging;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public partial class QueryAgent
{
	public QueryContext CurrentContext => ContextStack.Peek();

	internal readonly Stack<QueryContext> ContextStack = ((Func<Stack<QueryContext>>)(() =>
	{
		var stack = new Stack<QueryContext>();
		stack.Push(QueryContext.Script);
		return stack;
	}))();

	public string StatementKind { get; private set; } = TextType.General;

	private readonly List<string> _statements = new();

	private readonly List<string> _newlineBuffer = new();

	private Procedure? _currentProcedure;

	private static readonly string[] ContextSpecifiers =
	{
		".default", ".end", ".proc", ".script", ".module", ".init", ".static"
	};

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

		if (inputTrim.StartsWith('.') && ContextSpecifiers.Contains(inputTrim.Split().At(0)))
		{
			var context = inputTrim[1..];
			await SwitchContextAsync(context, output, setTranslated, handleDeletedFiles, update);
			return;
		}

		if (CurrentContext is QueryContext.Script)
			await RunScriptAsync(inputTrim, output, setTranslated, handleDeletedFiles, update);

		if (CurrentContext is QueryContext.AnonymousProcedure or QueryContext.NamedProcedure)
			_currentProcedure?.Push(inputTrim);
	}

	private async Task SwitchContextAsync(string contextStr, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update)
	{
		if (contextStr == "end" && ContextStack.Count > 1)
		{
			if (CurrentContext == QueryContext.AnonymousProcedure && _currentProcedure is not null)
			{
				ContextStack.Push(QueryContext.RunningProcedure);
				await _currentProcedure.RunAsync();
				ContextStack.Pop();
			}

			if (CurrentContext == QueryContext.NamedProcedure && _currentProcedure is not null)
				ModuleManager.UserDefined.Procedures.Add(_currentProcedure.Clone());


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

		if (contextStr.Split().At(0) == "proc")
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

			_currentProcedure = new(this, output, setTranslated, update, parameters, procName ?? string.Empty, ManualQuery.Cancellation.Token);
			ContextStack.Push(context);
			output.AddReflect($"Switched context to {CurrentContext}.");
		}
	}

	public async Task RunScriptAsync(string input, Buffer output, Action<string> setTranslated, Func<Task> handleDeletedFiles, Func<Task> update)
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
			await Preprocessor.ProcessAsync(inputTrim, output, handleDeletedFiles);
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
			var expr = inputTrim[(lParen + 1)..rParen];
			var comma = expr.LastIndexOf(',');
			var color = TextType.General;
			if (comma >= 0 && !Is.InsideStringLiteral(comma, 1, expr) && !Is.InsideBrace(comma, 1, expr, '{', '}') && !Is.InsideBrace(comma, 1, expr, '(', ')'))
				(expr, color) = (expr[..comma], expr[(comma + 1)..]);

			var printValue = await EvaluateExpressionAsync(expr);
			output.Add(To.String(printValue), color);
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

		if (WideVariableRegex.MemberAssign().IsMatch(inputTrim))
		{
			await AssignMember(inputTrim);
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

		var keySample = await EvaluateExpressionAsync(sample.Groups["key"].Value) ?? new object();
		var valueSample = await EvaluateExpressionAsync(sample.Groups["value"].Value + collectionEnd) ?? new object();

		var hashType = typeof(Dictionary<,>).MakeGenericType(keySample.GetType(), valueSample.GetType());

		var hashBase = Activator.CreateInstance(hashType);

		var add = hashType.GetMethod("Add");

		foreach (var match in matches)
		{
			var key = await EvaluateExpressionAsync(match.Groups["key"].Value);
			var value = await EvaluateExpressionAsync(match.Groups["value"].Value + collectionEnd);

			add?.Invoke(hashBase, new[] { key, value });
		}

		WideVariable.Variables[name] = hashBase;
	}

	private static async Task DefineVariableAsync(Match match, Buffer output, Action<string> setTranslated)
	{
		var name = match.Groups["name"].Value;
		var expr = match.Groups["expr"].Value;

		WideVariable.Variables[name] = await EvaluateExpressionAsync(expr);
	}

	private static int HashId = 0;
	private static readonly List<string> HashDeclarations = new();
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
	Script, Module, AnonymousProcedure, NamedProcedure, RunningProcedure
}