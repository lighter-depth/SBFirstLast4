using SBFirstLast4.Pages;
using System.Reflection;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

internal static class DynamicUtil
{
	internal static string ToFormat(this Exception ex) => ex switch
	{
		TargetInvocationException target => ToInternalExceptionFormat(target.InnerException!),
		AggregateException aggregate => aggregate.InnerExceptions.Select(ToInternalExceptionFormat).StringJoin(", "),
		_ => ToInternalExceptionFormat(ex)
	};
	private static string ToInternalExceptionFormat(Exception ex) => $"InternalException({ex.GetType().Name}): {ex.Message}";

	internal static string EscapeLiteral(this string str)
	{
		var sb = new StringBuilder();
		var isInsideLiteral = false;
		foreach (var c in str)
		{
			if (c == '"')
				isInsideLiteral = !isInsideLiteral;
			sb.Append(isInsideLiteral ? '0' : c);
		}
		return sb.ToString();
	}
}

internal static class TypeHelper
{
	internal static Type[] GetGenericArgumentsOrElementTypes(this Type? type)
		=> type is null ? throw new ArgumentNullException(nameof(type))
		: type.IsArray ? [type.GetElementType() ?? typeof(object)]
		: type.GetGenericArguments();

	internal static int GetParameterLength(this MethodInfo method) => method.GetParameters().Length;

	internal static IEnumerable<Type> GetParameterTypes(this MethodInfo method) => method.GetParameters().Select(p => p.ParameterType);
}

internal static class To
{
	internal static string String(object? result)
	{
		if (result is System.Collections.IDictionary dictionary)
			return $"%{{ {dictionary.Keys.OfType<object?>().Select(String).Zip(dictionary.Values.OfType<object?>().Select(String)).Select(t => $"{t.First} = {t.Second}").StringJoin(", ")} }}";

		if (result is System.Collections.IEnumerable enumerable and not string)
			return $"{{ {enumerable.OfType<object?>().Select(String).StringJoin(", ")} }}";

		return result?.ToString() ?? "null";
	}
}

internal static class Is
{
	internal static bool InsideStringLiteral(int startIndex, int length, string source)
	{
		if (startIndex < 0 || length < 0 || startIndex + length > source.Length)
			return false;

		var quoteCount = 0;
		for (var i = 0; i < startIndex; i++)
			if (source[i] == '"')
				quoteCount++;

		return quoteCount % 2 == 1;
	}

	internal static bool InsideBrace(int startIndex, int length, string source, char braceStart, char braceEnd)
	{
		if (startIndex < 0 || length < 0 || startIndex + length > source.Length)
			return false;

		var braceCount = 0;
		for (var i = 0; i < startIndex; i++)
		{
			if (source[i] == braceStart)
				braceCount++;
			else if (source[i] == braceEnd)
				braceCount--;
		}

		return braceCount % 2 == 1;
	}

	internal static bool SubclassOf(Type derivedType, string baseName)
	{
		if (derivedType.Name == baseName)
			return true;

		var type = derivedType;

		while (type != typeof(object))
		{
			type = type?.BaseType;

			if (type?.Name == baseName)
				return true;
		}

		return false;
	}
}

internal static class Find
{
	internal static (int Open, int Close)? OpenCloseBrace(string source, char openBrace, char closeBrace, int startIndex = 0)
	{
		source = EscapeLiterals(source);

		var index = source.IndexOf(openBrace, startIndex);
		if (index == -1)
			return null;

		var braceCount = 0;

		for (var i = index; i < source.Length; i++)
		{
			if (source[i] == openBrace)
			{
				braceCount++;
				continue;
			}
			if (source[i] == closeBrace)
			{
				braceCount--;
				if (braceCount == 0) return (index, i);
			}
		}
		return null;
	}

	internal static int CloseBrace(string source, char openBrace, char closeBrace, int startIndex = 0)
	{
		source = EscapeLiterals(source);

		var index = source.IndexOf(openBrace, startIndex);
		if (index == -1)
			return -1;

		var braceCount = 0;

		for (var i = index; i < source.Length; i++)
		{
			if (source[i] == openBrace)
			{
				braceCount++;
				continue;
			}
			if (source[i] == closeBrace)
			{
				braceCount--;
				if (braceCount == 0) return i;
			}
		}
		return -1;
	}

	internal static int FirstFree(string source, char target)
	{
		var index = 0;
		while (index < source.Length)
		{
			var result = source.IndexOf(target, index);
			if (result == -1)
				return result;

			if (Is.InsideBrace(result, 1, source, '(', ')')
			|| Is.InsideBrace(result, 1, source, '{', '}')
			|| Is.InsideStringLiteral(result, 1, source))
			{
				index = result + 1;
				continue;
			}
			return result;
		}
		return -1;
	}

	private static string EscapeLiterals(string source)
	{
		source = EscapeLiteral(source, '\'');
		source = EscapeLiteral(source, '"');
		source = EscapeLiteral(source, '`');
		return source;
	}

	private static string EscapeLiteral(string source, char quote)
	{
		var sb = new StringBuilder();
		var isInsideLiteral = false;
		foreach (var c in source)
		{
			if (c == quote)
				isInsideLiteral = !isInsideLiteral;
			sb.Append(isInsideLiteral ? '0' : c);
		}
		return sb.ToString();
	}
}

internal static class Split
{
	internal static IEnumerable<string> ParameterTexts(string parameterText)
	{
		var isInsideString = false;
		var charBuffer = new List<char>();
		for (var i = 0; i < parameterText.Length; i++)
		{
			var c = parameterText[i];

			if (c == '"')
				isInsideString = !isInsideString;

			if (c == '(' && !isInsideString)
			{
				var close = Find.CloseBrace(parameterText, '(', ')', i);
				charBuffer.AddRange(parameterText[i..(close + 1)]);
				i = close;
				continue;
			}
			if (c == '{' && !isInsideString)
			{
				var close = Find.CloseBrace(parameterText, '{', '}', i);
				charBuffer.AddRange(parameterText[i..(close + 1)]);
				i = close;
				continue;
			}

			if (c == ',' && !isInsideString)
			{
				yield return charBuffer.StringJoin();
				charBuffer.Clear();
				continue;
			}

			charBuffer.Add(c);

		}
		if (charBuffer.Count > 0)
			yield return charBuffer.StringJoin();
	}

	internal static IEnumerable<string> By(char separator, string source)
	{
		var isInsideString = false;
		var charBuffer = new List<char>();
		for (var i = 0; i < source.Length; i++)
		{
			var c = source[i];

			if (c == '"')
				isInsideString = !isInsideString;

			if (c == '<' && !isInsideString)
			{
				var close = Find.CloseBrace(source, '<', '>', i);
				charBuffer.AddRange(source[i..(close + 1)]);
				i = close;
				continue;
			}

			if (c == separator && !isInsideString)
			{
				yield return charBuffer.StringJoin();
				charBuffer.Clear();
				continue;
			}

			charBuffer.Add(c);

		}
		if (charBuffer.Count > 0)
			yield return charBuffer.StringJoin();
	}

	internal static IEnumerable<string> By(char separator, string source, BracketOption option)
	{
		var isInsideString = false;
		var charBuffer = new List<char>();
		var paren = option.HasFlag(BracketOption.Paren);
		var brace = option.HasFlag(BracketOption.Brace);
		var angle = option.HasFlag(BracketOption.Angle);
		var square = option.HasFlag(BracketOption.Square);


		for (var i = 0; i < source.Length; i++)
		{
			var c = source[i];

			if (c == '"')
				isInsideString = !isInsideString;

			if (paren && c == '(' && !isInsideString)
			{
				var close = Find.CloseBrace(source, '(', ')', i);
				charBuffer.AddRange(source[i..(close + 1)]);
				i = close;
				continue;
			}

			if (brace && c == '{' && !isInsideString)
			{
				var close = Find.CloseBrace(source, '{', '}', i);
				charBuffer.AddRange(source[i..(close + 1)]);
				i = close;
				continue;
			}

			if (angle && c == '<' && !isInsideString)
			{
				var close = Find.CloseBrace(source, '<', '>', i);
				charBuffer.AddRange(source[i..(close + 1)]);
				i = close;
				continue;
			}

			if (square && c == '[' && !isInsideString)
			{
				var close = Find.CloseBrace(source, '[', ']', i);
				charBuffer.AddRange(source[i..(close + 1)]);
				i = close;
				continue;
			}

			if (c == separator && !isInsideString)
			{
				yield return charBuffer.StringJoin();
				charBuffer.Clear();
				continue;
			}

			charBuffer.Add(c);

		}
		if (charBuffer.Count > 0)
			yield return charBuffer.StringJoin();
	}


	[Flags]
	internal enum BracketOption
	{
		None = 0,
		Paren = 1,
		Brace = 2,
		Angle = 4,
		Square = 8
	}
}

internal static class BufferEx
{
	public static void Add(this Buffer buffer, string content, string type) => buffer.Add((content, type));

	public static void AddReflect(this Buffer buffer, string content)
	{
		if (ManualQuery.IsReflect)
			buffer.Add(content, TextType.Monitor);
	}
}

internal static class TextType
{
	internal const string General = "",
						  Error = "Error",
						  Cmd = "Cmd",
						  Safe = "Safe",
						  Monitor = "Monitor";
}