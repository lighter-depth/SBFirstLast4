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
	private static string QueryOverWordDictionary(string input, string selector)
	{
		try
		{
			var config = new ParsingConfig { CustomTypeProvider = new SBCustomTypeProvider() };
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<Word>, object>(config, false, input);

			var result = expression.Compile().Invoke(SelectorHelper.ToWordEnumerable(selector));

			return ResultObjectToString(result);
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static string QueryOverStringDictionary(string input, string selector)
	{
		try
		{
			var config = new ParsingConfig { CustomTypeProvider = new SBCustomTypeProvider() };
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<string>, object>(config, false, input);

			var result = expression.Compile().Invoke(SelectorHelper.ToStringEnumerable(selector));

			return ResultObjectToString(result);
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}
	private static string QueryOverSingleton(string input)
	{
		try
		{
			var config = new ParsingConfig { CustomTypeProvider = new SBCustomTypeProvider() };
			var expression = DynamicExpressionParser.ParseLambda<IEnumerable<int>, object>(config, false, input);

			var result = expression.Compile().Invoke(_singletonEnumerable);

			return ResultObjectToString(result);
		}
		catch (Exception ex)
		{
			return $"Error: {ex.GetType().Name}: {ex.Message}";
		}
	}

	private static string ResultObjectToString(object result)
	{
		if (result is System.Collections.IEnumerable enumerable and not string)
			return $"[{string.Join(", ", enumerable.Cast<object>().Select(x => ResultObjectToString(x)))}]";

		return result.ToString() ?? "null";
	}
	private static readonly int[] _singletonEnumerable = new[] { 0 };
}
