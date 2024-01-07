using System.Linq.Dynamic.Core;

namespace SBFirstLast4.Dynamic;

public static class SBScriptExecutor
{
	public static Task<string> ExecuteAsync(string translated, string selector)
	{
		var dicType = SelectorHelper.GetDictionaryType(selector);
		return Task.Run(() => dicType switch
		{
			DictionaryType.String => QueryOverStringDictionary(translated, selector),
			DictionaryType.Word => QueryOverWordDictionary(translated, selector),
			_ => QueryOverSingleton(translated)
		});
	}

	public static Task<object?> ExecuteDynamicAsync(string translated, string selector)
	{
		var dicType = SelectorHelper.GetDictionaryType(selector);
		return Task.Run(() => dicType switch
		{
			DictionaryType.String => QueryOverStringDictionaryDynamic(translated, selector),
			DictionaryType.Word => QueryOverWordDictionaryDynamic(translated, selector),
			_ => QueryOverSingletonDynamic(translated)
		});
	}


	private static string QueryOverWordDictionary(string input, string selector)
		=> ResultObjectToString(QueryOverWordDictionaryDynamic(input, selector));

	private static string QueryOverStringDictionary(string input, string selector) 
		=> ResultObjectToString(QueryOverStringDictionaryDynamic(input, selector));

	private static string QueryOverSingleton(string input) 
		=> ResultObjectToString(QueryOverSingletonDynamic(input));
	

	private static object? QueryOverWordDictionaryDynamic(string input, string selector)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new SBCustomTypeProvider(),
				ResolveTypesBySimpleName = true,
				AllowNewToEvaluateAnyType = true
			};
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<Word>, object>(config, false, input);

			return expression.Compile().Invoke(SelectorHelper.ToWordEnumerable(selector));
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static object? QueryOverStringDictionaryDynamic(string input, string selector)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new SBCustomTypeProvider(),
				ResolveTypesBySimpleName = true,
				AllowNewToEvaluateAnyType = true
			};
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<string>, object>(config, false, input);

			return expression.Compile().Invoke(SelectorHelper.ToStringEnumerable(selector));
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static object? QueryOverSingletonDynamic(string input)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new SBCustomTypeProvider(),
				ResolveTypesBySimpleName = true,
				AllowNewToEvaluateAnyType = true
			};
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<int>, object>(config, false, input);

			return expression.Compile().Invoke(_singletonEnumerable);
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}

	private static string ResultObjectToString(object? result)
	{
		if (result is System.Collections.IEnumerable enumerable and not string)
			return $"[{enumerable.Cast<object>().Select(ResultObjectToString).Stringify()}]";

		return result?.ToString() ?? "null";
	}
	private static readonly int[] _singletonEnumerable = new[] { 0 };
}
