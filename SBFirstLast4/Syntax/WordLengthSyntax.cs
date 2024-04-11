using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Syntax;

public static partial class WordLengthSyntax
{

    public static Func<string, bool> ParseInputToPredicate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("入力が空です");

        var regex = SyntaxRegex();
        var match = regex.Match(input);

        // そもそもマッチしないパターン
        if (!match.Success)
            throw new ArgumentException("無効な構文です");

        var minValueStr = match.Groups[1].Value;
        var maxValueStr = match.Groups[2].Value;

        // カンマだけで数字がないパターン
        if (string.IsNullOrWhiteSpace(minValueStr) && string.IsNullOrWhiteSpace(maxValueStr))
            throw new ArgumentException("必ず境界を指定してください");

        int? minValue = string.IsNullOrWhiteSpace(minValueStr) ? null : int.Parse(minValueStr);
        int? maxValue = string.IsNullOrWhiteSpace(maxValueStr) ? null : int.Parse(maxValueStr);

        // 範囲がおかしいパターン (「5, 1」とか)
        if (minValue is not null && maxValue is not null && minValue > maxValue)
            throw new ArgumentException("無効な境界設定です");

        return (minValue, maxValue) switch
        {
            (not null, not null) => word => word.Length >= minValue && word.Length < maxValue,
            (not null, null) when input.Contains(',') => word => word.Length >= minValue,
            (not null, null) => word => word.Length == minValue,
            (null, not null) => word => word.Length < maxValue,
            _ => throw new ArgumentException("無効な構文です")
        };
    }
	public static Func<Word, bool> ParseInputToPredicateTyped(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			throw new ArgumentException("入力が空です");

		var regex = SyntaxRegex();
		var match = regex.Match(input);

		// そもそもマッチしないパターン
		if (!match.Success)
			throw new ArgumentException("無効な構文です");

		var minValueStr = match.Groups[1].Value;
		var maxValueStr = match.Groups[2].Value;

		// カンマだけで数字がないパターン
		if (string.IsNullOrWhiteSpace(minValueStr) && string.IsNullOrWhiteSpace(maxValueStr))
			throw new ArgumentException("必ず境界を指定してください");

		int? minValue = string.IsNullOrWhiteSpace(minValueStr) ? null : int.Parse(minValueStr);
		int? maxValue = string.IsNullOrWhiteSpace(maxValueStr) ? null : int.Parse(maxValueStr);

		// 範囲がおかしいパターン (「5, 1」とか)
		if (minValue is not null && maxValue is not null && minValue > maxValue)
			throw new ArgumentException("無効な境界設定です");

		return (minValue, maxValue) switch
		{
			(not null, not null) => word => word.Name.Length >= minValue && word.Name.Length < maxValue,
			(not null, null) when input.Contains(',') => word => word.Name.Length >= minValue,
			(not null, null) => word => word.Name.Length == minValue,
			(null, not null) => word => word.Name.Length < maxValue,
			_ => throw new ArgumentException("無効な構文です")
		};
	}
	public static bool TryParseInputToPredicate(string input, [NotNullWhen(true)]out Func<string, bool>? pred, [NotNullWhen(false)]out string? errorMsg)
    {
        (pred, errorMsg) = (null, null);
        try
        {
            pred = ParseInputToPredicate(input);
        }
        catch(ArgumentException ex)
        {
            errorMsg = ex.Message;
            return false;
        }
        return true;
    }

	public static bool TryParseInputToPredicate(string input, [NotNullWhen(true)] out Func<Word, bool>? pred, [NotNullWhen(false)] out string? errorMsg)
	{
		(pred, errorMsg) = (null, null);
		try
		{
			pred = ParseInputToPredicateTyped(input);
		}
		catch (ArgumentException ex)
		{
			errorMsg = ex.Message;
			return false;
		}
		return true;
	}

	[GeneratedRegex(@"^\s*(\d*)\s*,?\s*(\d*)\s*$")]
    private static partial Regex SyntaxRegex();
}
