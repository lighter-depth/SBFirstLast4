using Antlr4.Runtime;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public sealed class Procedure
{
	public string Name { get; }

	public List<string> Parameters { get; }

	public Guid Id { get; init; } = Guid.NewGuid();

	public string ModuleName { get; init; }

	private List<string> _lines = new();

	private Buffer _buffer;

	private Action<string> _setTranslated;

	private Func<Task> _update;

	public QueryAgent Agent { get; internal set; }

	private CancellationToken _token;

	public string Value => _lines.StringJoin(Environment.NewLine);

	internal static readonly QueryAgent DiscardAgent = new();

	internal static readonly Buffer DiscardBuffer = new();

	internal static readonly Func<Task> DiscardHandleDeletedFiles = () => Task.CompletedTask;

	internal static readonly Action<string> DiscardSetTranslated = _ => { };

	internal static readonly Func<Task> DiscardUpdate = () => Task.CompletedTask;

	public Procedure(string name, List<string> parameters, string moduleName, QueryAgent agent, Buffer buffer, Action<string> setTranslated, Func<Task> update, CancellationToken token)
		=> (Name, Parameters, ModuleName, Agent, _buffer, _setTranslated, _update, _token) = (name, parameters, moduleName, agent, buffer, setTranslated, update, token);

	public Procedure Clone() => new(Name, Parameters, ModuleName, Agent, _buffer, _setTranslated, _update, _token) { Id = Id, _lines = _lines.ToList() };

	public void Update(QueryAgent agent, Buffer buffer, Action<string> setTranslated, Func<Task> update, CancellationToken token)
		=> (Agent, _buffer, _setTranslated, _update, _token) = (agent, buffer, setTranslated, update, token);

	public void Push(string line) => _lines.Add(line.Trim());

	public void ExpandTransient(Macro transient) => _lines = _lines.Select(l => Transient.Expand(l, transient)).ToList();

	private async Task<string> GetSourceTextAsync()
	{
		var textBase = await GetProcessedTextAsync();
		if (textBase.Contains('^'))
			textBase = WideVariableRegex.InternalReference().Replace(textBase, m =>
			{
				if (Is.InsideStringLiteral(m.Index, m.Length, textBase))
					return m.Value;

				return $"&__proc_internal_{m.Groups["name"].Value}_{Name}_{Id:N}_generated";
			});
		textBase = ReplaceUnless(textBase);
		textBase = textBase.ReplaceFreeString("forever", "for (;;)");
		return textBase;
	}

	private async Task<string> GetProcessedTextAsync()
	{
		var lines = HandleEphemerals(_lines);
		var result = new List<string>();
		await foreach (var line in HandleDirectives(lines))
			result.Add(line);
		return result.StringJoin(Environment.NewLine);
	}

	private static IEnumerable<string> HandleEphemerals(IEnumerable<string> lines)
	{
		foreach (var line in lines)
		{
			if (!(line.StartsWith("#ephemeral") || line.StartsWith("#evaporate")))
			{
				yield return Macro.ExpandEphemeral(line);
				continue;
			}
			Preprocessor.ProcessEphemeral(line, DiscardBuffer);
		}
	}

	private static async IAsyncEnumerable<string> HandleDirectives(IEnumerable<string> lines)
	{
		foreach (var line in lines)
		{
			if (!line.StartsWith('#'))
			{
				var result = Macro.Expand(line);
				yield return result;
				continue;
			}
			await Preprocessor.ProcessAsync(line, DiscardBuffer, DiscardHandleDeletedFiles);
		}
	}

	private static string ReplaceUnless(string source)
	{
		while (source.Contains("unless"))
		{
			var unless = source.IndexOf("unless");
			var openClose = Find.OpenCloseBrace(source, '(', ')', unless);
			if (openClose is null) return source;
			var (open, close) = openClose.Value;

			var cond = source[open..(close + 1)];

			var sb = new StringBuilder(source);
			sb.Remove(unless, close - unless + 1);
			sb.Insert(unless, $"if (!{cond})");
			source = sb.ToString();
		}
		return source;
	}

	public async Task<object?> RunAsync(List<object?>? args = null)
	{
		if ((args?.Count ?? default) != Parameters.Count)
			return string.Empty;

		Agent.ContextStack.Push(QueryContext.RunningProcedure);

		if (args is not null)
			foreach (var (name, arg) in Parameters.Zip(args))
			{
				if (name.StartsWith('&'))
				{
					var wideName = name[1..];
					WideVariable.Variables[wideName] = arg;
					continue;
				}
				var internalName = $"__proc_internal_{name[1..]}_{Name}_{Id:N}_generated";
				WideVariable.Variables[internalName] = arg;
			}

		var source = await GetSourceTextAsync();
		var stream = new AntlrInputStream(source);

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

		var tree = parser.script();

		if (listener_lexer.HadError || listener_parser.HadError)
		{
			var isLexerError = listener_lexer.HadError;
			var line = isLexerError ? listener_lexer.Line : listener_parser.Line;
			var col = isLexerError ? listener_lexer.Column : listener_parser.Column;
			var msg = isLexerError ? listener_lexer.Message : listener_parser.Message;

			_buffer.Add($"COMPILE ERROR: {msg} at {line}:{col}", TextType.Error);
			Agent.ContextStack.Pop();
			return "COMPILE ERROR";
		}

		if (ManualQuery.IsReflect)
			_buffer.Add(tree.ToStringTree(parser), TextType.Monitor);

		try
		{
			var visitor = new SBProcLangVisitor(_buffer, _setTranslated, _update, _token);
			var result = await visitor.Visit(tree);

			if (ManualQuery.IsReflect)
				_buffer.Add(To.String(result), TextType.Monitor);

			Agent.ContextStack.Pop();
			return result;
		}
		catch (Exception ex)
		{
			_buffer.Add($"InternalException({ex.GetType().Name}): {ex.Message}", TextType.Error);
			Agent.ContextStack.Pop();
			return ex.Stringify();
		}
	}
}
