using SBFirstLast4.Pages;
using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

internal class To
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

internal class Is
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
