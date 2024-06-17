using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4.Syntax;

public static class StringEscapeSyntax
{
	public static bool TryParse(string input, [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out string? errorMsg)
	{
		(output, errorMsg) = (null, null);
		try
		{
			output = Parse(input);
			return true;
		}
		catch(Exception ex)
		when(ex is FormatException or IndexOutOfRangeException)
		{
			errorMsg = ex.Message;
			return false;
		}
	}

	public static string Parse(string input)
	{
		var length = input.Length;
		var index = 0;
		var sb = new StringBuilder();

		while(index < length) 
		{
			var c = input.At(index);
			if (c is not '\\')
			{
				sb.Append(c);
				index++;
				continue;
			}
			index++;
			var escape = input.At(index);
			if(escape is 'u')
			{
				var unicode = input.Take((index + 1)..(index + 5)).StringJoin();
				try
				{
					var code = Convert.ToChar(Convert.ToUInt32(unicode, 16));
					sb.Append(code);
				}
				catch(Exception ex)
				when(ex is ArgumentException or ArgumentOutOfRangeException or FormatException or OverflowException)
				{
					throw new FormatException($"無効なエスケープシーケンス '\\{unicode}' が含まれています");
				}

				index += 5;
				continue;
			}
			if(escape is 'U')
			{
				var unicode = input.Take((index + 1)..(index + 9)).StringJoin();
				try
				{
					var code = Convert.ToChar(Convert.ToUInt64(unicode, 16));
					sb.Append(code);
				}
				catch (Exception ex)
				when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException or OverflowException)
				{
					throw new FormatException($"無効なエスケープシーケンス '\\{unicode}' が含まれています");
				}
				index += 9;
				continue;
			}
			sb.Append(escape switch
			{
				'\\' => '\\',
				'0' => '\0',
				'a' => '\a',
				'b' => '\b',
				'e' => '\x1b',
				'f' => '\f',
				'n' => '\n',
				'r' => '\r',
				's' => ' ',
				'S' => '　',
				't' => '\t',
				'v' => '\v',
				_ => throw new FormatException($"無効なエスケープシーケンス '\\{escape}' が含まれています")
			});
			index++;
		}
		return sb.ToString();
	}
}
