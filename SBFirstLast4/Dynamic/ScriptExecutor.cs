using System.Linq.Dynamic.Core;

namespace SBFirstLast4.Dynamic;

public static class ScriptExecutor
{
	public static string Execute(string translated, string selector)
		=> StringUtil.ToString(ExecuteDynamic(translated, selector));

	public static object? ExecuteDynamic(string translated, string selector)
		=> SelectorHelper.GetDictionaryType(selector) switch
		{
			DictionaryType.String => QueryOverStringDictionaryDynamic(translated, selector),
			DictionaryType.Word => QueryOverWordDictionaryDynamic(translated, selector),
			_ => QueryOverSingletonDynamic(translated)
		};

	private static object? QueryOverWordDictionaryDynamic(string input, string selector)
	{
		try
		{
			var config = new ParsingConfig
			{
				CustomTypeProvider = new CustomTypeProvider(),
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
				CustomTypeProvider = new CustomTypeProvider(),
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
				CustomTypeProvider = new CustomTypeProvider(),
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


	/// <summary>
	/// クエリ ソース用のSO辞書
	/// </summary>
	/// <seealso cref="Extensions.DynamicExtensionHelper.GetSingleton"/>
	private static readonly int[] _singletonEnumerable = { 0 };
}

internal class StringUtil
{
	internal static string ToString(object? result)
	{
		if (result is System.Collections.IEnumerable enumerable and not string)
			return $"[{enumerable.Cast<object>().Select(ToString).StringJoin()}]";

		return result?.ToString() ?? "null";
	}
}
