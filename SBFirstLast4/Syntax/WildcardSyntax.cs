using System.Diagnostics.CodeAnalysis;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Syntax;

[DynamicLinqType]
public static class WildcardSyntax
{
	public static bool IsValid(string input) => TryParse(input, out _);

	public static Regex? ParseRegex(string input) => TryParse(input, out var result) ? new(result) : null;

	public static string Parse(string input) => TryParse(input, out var result) ? result : string.Empty;

	public static bool TryParse(string input, [NotNullWhen(true)] out string? str)
	{
		str = null;
		var replaced = input
			.Replace('?', '.')
			.Replace('？', '.')
			.Replace("*", ".*")
			.Replace("＊", ".*")
			.Where(c => c is '.' or '*' or (>='\u3040' and <'\u30FF'))
			.StringJoin();

		var pattern = $"^{replaced}$";

		if (!Utils.IsValidRegex(pattern))
			return false;

		str = pattern;
		return true;
	}
}