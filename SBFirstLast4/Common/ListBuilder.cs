using System.Text.RegularExpressions;

namespace SBFirstLast4;

public static class ListBuilder
{
	public static string Build(Word[] words, WordType omitType, string wordCount, ListType listType, SortArg sortArg, ListFormat formatType, WordCountFormat wordCountFormat)
	{
		var sb = new StringBuilder();

		if (formatType != ListFormat.SimulatorCsv)
			sb.Append($"/*{Environment.NewLine} * このリストは、機械的に生成されたものです。{Environment.NewLine} * 実際のゲーム内容とは差異がある可能性があります。{Environment.NewLine} */{Environment.NewLine}{Environment.NewLine}");

		foreach (var i in Utils.KanaList)
		{
			sb.Append(ToHeader(i[0], formatType));
			for (var j = 0; j < i.Length; j++)
			{
				var key = i[j];
				var format = new Regex(listType switch
				{
					ListType.FirstChar => $"^.*{key}ー*$",
					_ => $"^{key}.*$"
				});

				var filtered = words
					.Where(x => format.IsMatch(x.Name))
					.SortByLength(sortArg)
					.Select(x => x.ToFormat(formatType, omitType))
					.ToList();

				var takeCount = filtered.Count;

				if (wordCountFormat == WordCountFormat.Custom
				&& listType != ListType.TypedOnly
				&& int.TryParse(wordCount, out var tmp))
					takeCount = tmp;

				if (filtered.Count != 0)
					sb.Append(BuildRow(filtered.Take(takeCount).ToList(), formatType));

				else if (formatType == ListFormat.FormalWiki)
					sb.Append($"・{Environment.NewLine}");
			}
		}
		return sb.ToString();
	}

	public static string Build(string[] words, string wordCount, ListType listType, SortArg sortArg, ListFormat formatType, WordCountFormat wordCountFormat)
	{
		var sb = new StringBuilder();
		if (formatType != ListFormat.SimulatorCsv)
			sb.Append($"/*{Environment.NewLine} * このリストは、機械的に生成されたものです。{Environment.NewLine} * 実際のゲーム内容とは差異がある可能性があります。{Environment.NewLine} */{Environment.NewLine}{Environment.NewLine}");
		foreach (var i in Utils.KanaList)
		{
			sb.Append(ToHeader(i[0], formatType));
			for (var j = 0; j < i.Length; j++)
			{
				var key = i[j];
				var format = new Regex(listType switch
				{
					ListType.FirstChar => $"^.*{key}ー*$",
					_ => $"^{key}.*$"
				});
				var filtered = words
					.Where(x => format.IsMatch(x))
					.SortByLength(sortArg)
					.ToList();

				var takeCount = filtered.Count;

				if (wordCountFormat == WordCountFormat.Custom
				&& listType != ListType.TypedOnly
				&& int.TryParse(wordCount, out var tmp)) takeCount = tmp;

				if (filtered.Count != 0)
					sb.Append(BuildRow(filtered.Take(takeCount).ToList(), formatType));

				else if (formatType == ListFormat.FormalWiki)
					sb.Append($"・{Environment.NewLine}");
			}
		}
		return sb.ToString();
	}

	public static string BuildSingle(string[] words, string wordCount, SortArg sortArg, ListFormat formatType, WordCountFormat wordCountFormat)
	{
		var sb = new StringBuilder();

		if (formatType != ListFormat.SimulatorCsv)
			sb.Append($"/*{Environment.NewLine} * このリストは、機械的に生成されたものです。{Environment.NewLine} * 実際のゲーム内容とは差異がある可能性があります。{Environment.NewLine} */{Environment.NewLine}{Environment.NewLine}");

		var filtered = words.SortByLength(sortArg).ToList();
		var takeCount = filtered.Count;

		if (wordCountFormat == WordCountFormat.Custom && int.TryParse(wordCount, out var tmp))
			takeCount = tmp;

		if (filtered.Count != 0)
			sb.Append(BuildRow(filtered.Take(takeCount).ToList(), formatType));

		else if (formatType == ListFormat.FormalWiki) sb.Append($"・{Environment.NewLine}");

		return sb.ToString();
	}

	private static string BuildRow(List<string> filtered, ListFormat formatType)
	{
		var header = formatType is not ListFormat.SimulatorCsv ? "・" : string.Empty;
		var splitter = formatType is not ListFormat.SimulatorCsv ? "、" : Environment.NewLine;
		var footer = "  " + Environment.NewLine;
		var sb = new StringBuilder();
		sb.Append(header);
		sb.Append(filtered[0]);
		for (var i = 1; i < filtered.Count; i++)
		{
			sb.Append(splitter);
			sb.Append(filtered[i]);
		}
		sb.Append(footer);
		return sb.ToString();
	}

	private static string ToHeader(string keyStr, ListFormat formatType) => formatType switch
	{
		ListFormat.SlashBracket => $"【{keyStr}行】{Environment.NewLine}",
		ListFormat.SimulatorCsv => string.Empty,
		_ => $"**{keyStr}行{Environment.NewLine}"
	};
}
public enum ListDeclType
{
	None, FirstLast, First, Last, Regex, TypedOnly, Other
}
public enum ListType
{
	None, LastChar, FirstChar, TypedOnly
}

public enum SortArg
{
	NoConstraint, HopefullyMoreThanSeven, OnlyMoreThanSeven
}
public enum ListFormat
{
	InformalWiki, FormalWiki, SlashBracket, SimulatorCsv
}

public enum WordCountFormat
{
	Custom, All
}