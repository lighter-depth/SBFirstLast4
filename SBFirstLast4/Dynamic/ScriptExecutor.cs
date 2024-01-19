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

	private static object? QueryOverWordDictionaryDynamic(string? input, string? selector)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new CustomTypeProvider(),
				AllowNewToEvaluateAnyType = true
			};
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<Word>, object>(config, false, input ?? string.Empty);

			return expression.Compile().Invoke(SelectorHelper.ToWordEnumerable(selector ?? string.Empty));
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static object? QueryOverStringDictionaryDynamic(string? input, string? selector)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new CustomTypeProvider(),
				AllowNewToEvaluateAnyType = true
			};
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<string>, object>(config, false, input ?? string.Empty);

			return expression.Compile().Invoke(SelectorHelper.ToStringEnumerable(selector ?? string.Empty));
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static object? QueryOverSingletonDynamic(string? input)
	{
		try
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
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}


	/// <summary>
	/// SO dictionary for query source
	/// </summary>
	/// <seealso cref="Extensions.DynamicExtensionHelper.GetSingleton"/>
	private static readonly int[] _singletonEnumerable = { 0 };
}
