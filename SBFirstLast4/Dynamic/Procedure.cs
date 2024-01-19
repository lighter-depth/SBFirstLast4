using Antlr4.Runtime;
using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public class Procedure
{
	private List<string> _lines = new();

	public void Push(string line) => _lines.Add(line.Trim());

	public void Clear() => _lines = new();

	public async Task FlushAsync(Buffer outputBuffer, Action<string> setTranslated, Func<Task> update)
	{
		var source = _lines.StringJoin(Environment.NewLine);
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

			outputBuffer.Add($"COMPILE ERROR: {msg} at {line}:{col}", TextType.Error);
			return;
		};

		if (ManualQuery.IsReflect)
			outputBuffer.Add(tree.ToStringTree(parser), TextType.Monitor);

		try
		{
			var visitor = new SBProcLangVisitor(outputBuffer, setTranslated, update);
			var result = await visitor.Visit(tree);

			if (ManualQuery.IsReflect)
				outputBuffer.Add(To.String(result), TextType.Monitor);
		}
		catch(Exception ex)
		{
			outputBuffer.Add($"InternalException({ex.GetType().Name}): {ex.Message}", TextType.Error);
		}
	}
}
