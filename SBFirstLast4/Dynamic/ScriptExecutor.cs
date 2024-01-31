using Antlr4.Runtime;
using Microsoft.CSharp.RuntimeBinder;
using System.Linq.Dynamic.Core;

namespace SBFirstLast4.Dynamic;

public static class ScriptExecutor
{
	public static string Execute(string? translated, string? selector)
		=> To.String(ExecuteDynamic(translated, selector));

	public static object? ExecuteDynamic(string? translated, string? selector)
		=> SelectorHelper.GetDictionaryType(selector) switch
		{
			DictionaryType.String => QueryOverStringDictionaryDynamic(translated, selector),
			DictionaryType.Word => QueryOverWordDictionaryDynamic(translated, selector),
			_ => QueryOverSingletonDynamic(translated)
		};

	public static async Task<(bool Success, object? Result)> EvaluateSimpleExpressionAsync(string? input)
	{
		input ??= string.Empty;

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

		var tree = parser.expr();

		if (listener_lexer.HadError || listener_parser.HadError)
			return (false, null);

		try
		{
			var visitor = new SBProcLangVisitor();
			var result = await visitor.Visit(tree);
			return (true, result);
		}
		catch (Exception e)
		when (e is NullReferenceException or SBProcLangVisitorException or RuntimeBinderException)
		{
			return (false, null);
		}
	}

	private static object? QueryOverWordDictionaryDynamic(string? input, string? selector)
	{
		var config = new ParsingConfig
		{
			CustomTypeProvider = new CustomTypeProvider(),
			AllowNewToEvaluateAnyType = true
		};
		var expression = DynamicExpressionParser.ParseLambda<IEnumerable<Word>, object>(config, false, input ?? string.Empty);

		return expression.Compile().Invoke(SelectorHelper.ToWordEnumerable(selector ?? string.Empty));
	}
	private static object? QueryOverStringDictionaryDynamic(string? input, string? selector)
	{
		var config = new ParsingConfig
		{
			CustomTypeProvider = new CustomTypeProvider(),
			AllowNewToEvaluateAnyType = true
		};
		var expression = DynamicExpressionParser.ParseLambda<IEnumerable<string>, object>(config, false, input ?? string.Empty);

		return expression.Compile().Invoke(SelectorHelper.ToStringEnumerable(selector ?? string.Empty));

	}
	private static object? QueryOverSingletonDynamic(string? input)
	{
		var config = new ParsingConfig
		{
			CustomTypeProvider = new CustomTypeProvider(),
			ResolveTypesBySimpleName = true,
			AllowNewToEvaluateAnyType = true
		};
		var expression = DynamicExpressionParser.ParseLambda<IEnumerable<int>, object>(config, false, input ?? string.Empty);

		return expression.Compile().Invoke(_singletonEnumerable);
	}


	/// <summary>
	/// SO dictionary for query source
	/// </summary>
	/// <seealso cref="Extensions.DynamicExtensionHelper.GetSingleton"/>
	private static readonly int[] _singletonEnumerable = { 0 };
}
