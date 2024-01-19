using Antlr4.Runtime;
using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4.Dynamic;

public class ErrorListener<S> : ConsoleErrorListener<S>
{
	public bool HadError { get; private set; }

	public int Line { get; private set; }

	public int Column { get; private set; }

	public string? Message { get; private set; }

	public override void SyntaxError(TextWriter output, IRecognizer recognizer, S offendingSymbol, int line,
		int col, string msg, RecognitionException e)
	{
		HadError = true;
		Line = line;
		Column = col;
		Message = msg;
	}
}
