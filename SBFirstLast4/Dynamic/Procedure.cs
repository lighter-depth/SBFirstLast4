using sly.lexer;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public class Procedure
{
	private readonly List<string> _statements = new();

	private readonly ILexer<ProcedureToken> _lexer = LexerBuilder.BuildLexer<ProcedureToken>().Result;

	public void Push(string statement) => _statements.Add(statement.Trim());

	public async Task FlushAsync(Buffer outputBuffer, Action<string> setTranslated, Func<Task> handleDeletedFiles)
	{
		var agent = new QueryAgent();

		await Task.CompletedTask;

		var code = _statements.StringJoin(Environment.NewLine);

		if (_lexer.Tokenize(code) is var lexerResult && lexerResult.IsError)
			throw new InvalidProcedureException(lexerResult.Error.ToString());

		var tokens = lexerResult.Tokens;


		var context = TokenReaderContext.Semicolon;

		for(var i = 0; i < tokens.Count;)
		{
			Token<ProcedureToken> token() => tokens[i];
			var id = token().TokenID;
			context = Token.ControlFlow.Contains(id) ? TokenReaderContext.Block : TokenReaderContext.Semicolon;
			
		}
	}

	private enum TokenReaderContext
	{
		Semicolon, Block
	}
}

internal record Statement(List<Token<ProcedureToken>> Tokens);


public enum ProcedureToken
{
	#region keywords 0 -> 19

	[Lexeme("(if)")] IF = 1,

	[Lexeme("(unless)")] UNLESS = 2,

	[Lexeme("(else)")] ELSE = 3,

	[Lexeme("(while)")] WHILE = 4,

	[Lexeme("(do)")] DO = 5,

	[Lexeme("(for)")] FOR = 6,

	[Lexeme("(true)")] TRUE = 7,

	[Lexeme("(false)")] FALSE = 8,

	[Lexeme("(null)")] NULL = 9,

	[Lexeme("(and)")] AND = 10,

	[Lexeme("(input)")] INPUT = 11,

	[Lexeme("(print)")] PRINT = 12,

	[Lexeme("(return)")] RETURN = 13,

	[Lexeme("(break)")] BREAK = 14,

	[Lexeme("(continue)")] CONTINUE = 15,

	[Lexeme("(redo)")] REDO = 16,

	[Lexeme("(until)")] UNTIL = 17,

	#endregion

	#region literals 20 -> 29

	[Lexeme("[a-zA-Z][0-9a-z_A-Z]*")] IDENTIFIER = 20,

	[Lexeme("\"[^\"]*\"")] STRING = 21,

	//[Lexeme("(?:0b|0x)?[0-9]+(?:L|U|UL)?")] INT = 22,

	[Lexeme("&[a-zA-Z][0-9a-z_A-Z]*")] WIDE_VARIABLE = 23,

	#endregion

	#region operators 30 -> 49

	[Lexeme(">")] GREATER = 30,

	[Lexeme("<")] LESSER = 31,

	[Lexeme("==")] EQUALS = 32,

	[Lexeme("!=")] DIFFERENT = 33,

	[Lexeme("\\.")] MEMBER_ACCESS = 34,

	[Lexeme("=")] ASSIGN = 35,

	[Lexeme("\\+")] PLUS = 36,

	[Lexeme("\\-")] MINUS = 37,


	[Lexeme("\\*")] TIMES = 38,

	[Lexeme("\\/")] DIVIDE = 39,

	[Lexeme("%")] MODULUS = 40,

	#endregion

	#region sugar 50 ->

	[Lexeme("\\(")] LPAREN = 50,

	[Lexeme("\\)")] RPAREN = 51,

	[Lexeme(";")] SEMICOLON = 52,

	[Lexeme("[ \\t]+", true)] WS = 53,

	[Lexeme("[\\n\\r]+", true, true)] EOL = 54,

	[Lexeme("\\{")] LBRACE = 55,

	[Lexeme("\\}")] RBRACE = 56,

	[Lexeme(".*")] UNKNOWN = 57,

	EOF = 0

	#endregion
}

internal static class Token
{
	internal static readonly ProcedureToken[] ControlFlow =
	{
		ProcedureToken.IF, ProcedureToken.ELSE, ProcedureToken.UNLESS, ProcedureToken.WHILE, ProcedureToken.DO, ProcedureToken.UNTIL, ProcedureToken.FOR
	};
}

public class InvalidProcedureException : Exception
{
	public InvalidProcedureException(string message) : base(message) { }
}
