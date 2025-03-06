using SBFirstLast4.Syntax;
using System.Diagnostics.CodeAnalysis;
using static SBFirstLast4.ListLoader;

namespace SBFirstLast4;


/*
 * 単語帳サンプル
 * 
 * ...
 * [listOpen]
 * りんご[wordJoint][typeOpen][typeFormat(食べ物)][typeSeparator][typeFormat(植物)][typeClose]
 * [wordSeparator]
 * ごりら[wordJoint][typeOpen][typeFormat(動物)][typeClose]
 * [wordSeparator]
 * ...
 * [listClose]
 * ...
 */

public sealed class ListLoader(string? listOpen, string? listClose, string? wordSeparator, string wordJoint, WordTypeFormat typeFormat, string? typeOpen, string typeSeparator, string? typeClose, string[] omitHeaders)
{
	private readonly string? _listOpen = listOpen;

	private readonly string? _listClose = listClose;

	private readonly string? _wordSeparator = wordSeparator;

	private readonly string _wordJoint = wordJoint;

	private readonly WordTypeFormat _typeFormat = typeFormat;

	private readonly string? _typeOpen = typeOpen;

	private readonly string _typeSeparator = typeSeparator;

	private readonly string? _typeClose = typeClose;

	private readonly string[] _omitHeaders = omitHeaders;

	public static readonly ListLoader DefaultFormat = new ListLoaderBuilder().Build();

	private const string DataOpen = ".data";

	/* .format
	 * open: _
	 * close: _
	 * separator: _
	 * joint: _
	 * typename: _
	 * typeopen: _
	 * typejoint: _
	 * typeclose: _
	 * omit: _, _, _
	 * 
	 * .data
	 */
	public static ListLoader CreateFormat(string input)
	{
		var formatClose = input.IndexOf(DataOpen);
		if (formatClose == -1)
			return DefaultFormat;

		var format = input[..formatClose];
		using var reader = new StringReader(format);

		var builder = new ListLoaderBuilder();

		while(reader.Peek() > -1)
		{
			var line = reader.ReadLine();
			if (line is null)
				break;

			var colon = line.IndexOf(':');
			if (colon == -1 || colon >= line.Length - 2)
				continue;

			var title = line.AsSpan()[..colon];
			var value = line[(colon + 1)..];
			ApplyFormat(builder, title, StringEscapeSyntax.Parse(value));
		}

		return builder.Build();
	}

	public (ICollection<string> TL, ICollection<Word> TD) LoadMixed(string input)
	{
		var source = input;

		var dataIndex = source.IndexOf(DataOpen);

		if (dataIndex != -1)
			source = source[(dataIndex + DataOpen.Length)..];

		if (!string.IsNullOrEmpty(_listOpen))
		{
			var index = source.IndexOf(_listOpen);
			if (index == -1)
				return ([], []);
			source = source[(index + _listOpen.Length)..];
		}

		if (!string.IsNullOrEmpty(_listClose))
		{
			var index = source.IndexOf(_listClose);
			if (index == -1)
				return ([], []);
			source = source[..index];
		}

		var tlResult = new List<string>();
		var tdResult = new List<Word>();

		var lines = source.Split(["\r\n", "\n"], StringSplitOptions.None).WhereWhen(l => !IsOmittable(l), _omitHeaders.Length > 0);

		foreach (var i in lines)
			foreach (var j in i.SplitIfNotNull(_wordSeparator))
			{
				var joint = j.IndexOf(_wordJoint);
				if (joint == -1)
				{
					if (!string.IsNullOrWhiteSpace(j))
						tlResult.Add(j);
					continue;
				}

				var wordName = j[..joint];
				var strTypes = j[(joint + 1)..];

				if (string.IsNullOrWhiteSpace(strTypes))
				{
					tlResult.Add(wordName);
					continue;
				}

				var typeOpen = 0;
				if (_typeOpen?.Length is { } openLen and > 0)
					typeOpen = strTypes.IndexOf(_typeOpen) + openLen;

				var typeClose = strTypes.Length;
				if (_typeClose?.Length is { } closeLen and > 0)
					typeClose = strTypes.IndexOf(_typeClose);

				var separatorIndex = strTypes.IndexOf(_typeSeparator);
				if (separatorIndex == -1)
				{
					var type = strTypes.AsSpan()[typeOpen..typeClose].SpanToType(_typeFormat);
					tdResult.Add(new(wordName, type, WordType.Empty));
					continue;
				}

				var type1 = strTypes.AsSpan()[typeOpen..separatorIndex].SpanToType(_typeFormat);
				var type2 = strTypes.AsSpan()[(separatorIndex + _typeSeparator.Length)..typeClose].SpanToType();
				tdResult.Add(new(wordName, type1, type2));

			}
		return (tlResult, tdResult);
	}

	public ICollection<string> LoadTL(string input)
	{
		var source = input;

		var dataIndex = source.IndexOf(DataOpen);

		if (dataIndex != -1)
			source = source[(dataIndex + DataOpen.Length)..];

		if (!string.IsNullOrEmpty(_listOpen))
		{
			var index = source.IndexOf(_listOpen);
			if (index == -1)
				return [];
			source = source[(index + _listOpen.Length)..];
		}

		if (!string.IsNullOrEmpty(_listClose))
		{
			var index = source.IndexOf(_listClose);
			if (index == -1)
				return [];
			source = source[..index];
		}

		var result = new List<string>();

		var lines = source.Split(["\r\n", "\n"], StringSplitOptions.None).WhereWhen(l => !IsOmittable(l), _omitHeaders.Length > 0);

		foreach (var i in lines)
			result.AddRange(i.Split(_wordSeparator));

		return result;
	}

	public ICollection<Word> LoadTD(string input)
	{
		var source = input;

		var dataIndex = source.IndexOf(DataOpen);

		if (dataIndex != -1)
			source = source[(dataIndex + DataOpen.Length)..];

		if (!string.IsNullOrEmpty(_listOpen))
		{
			var index = source.IndexOf(_listOpen);
			if (index == -1)
				return [];
			source = source[(index + _listOpen.Length)..];
		}

		if (!string.IsNullOrEmpty(_listClose))
		{
			var index = source.IndexOf(_listClose);
			if (index == -1)
				return [];
			source = source[..index];
		}

		var result = new List<Word>();

		var lines = source.Split(["\r\n", "\n"], StringSplitOptions.None).WhereWhen(l => !IsOmittable(l), _omitHeaders.Length > 0);

		foreach (var i in lines)
			foreach (var j in i.SplitIfNotNull(_wordSeparator))
			{
				var joint = j.IndexOf(_wordJoint);
				if (joint == -1)
					continue;

				var wordName = j[..joint];
				var strTypes = j[(joint + 1)..];

				if (string.IsNullOrWhiteSpace(strTypes))
				{
					result.Add((Word)wordName);
					continue;
				}

				var typeOpen = 0;
				if (_typeOpen?.Length is { } openLen and > 0)
					typeOpen = strTypes.IndexOf(_typeOpen) + openLen;

				var typeClose = strTypes.Length;
				if (_typeClose?.Length is { } closeLen and > 0)
					typeClose = strTypes.IndexOf(_typeClose);

				var separatorIndex = strTypes.IndexOf(_typeSeparator);
				if (separatorIndex == -1)
				{
					var type = strTypes.AsSpan()[typeOpen..typeClose].SpanToType(_typeFormat);
					result.Add(new(wordName, type, WordType.Empty));
					continue;
				}

				var type1 = strTypes.AsSpan()[typeOpen..separatorIndex].SpanToType(_typeFormat);
				var type2 = strTypes.AsSpan()[(separatorIndex + _typeSeparator.Length)..typeClose].SpanToType();
				result.Add(new(wordName, type1, type2));
			}

		return result;
	}

	public static ICollection<string> LoadTL_A(string input)
		=> input.Trim().Split(["\r\n", "\n"], StringSplitOptions.None);

	public static ICollection<Word> LoadTD_A(string input)
		=> input.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(x => x.Split())
			.Select(x => new Word(x.At(0), x.At(1), x.At(2)))
			.ToArray();

	private bool IsOmittable(string s)
	{
		foreach (var i in _omitHeaders)
			if (s.StartsWith(i, StringComparison.Ordinal))
				return true;

		return false;
	}

	private static void ApplyFormat(ListLoaderBuilder builder, ReadOnlySpan<char> title, string value)
	{
		switch (title)
		{
			case "open":
				builder.ListOpen = NotNullIfNotNull(value);
				return;

			case "close":
				builder.ListClose = NotNullIfNotNull(value);
				return;

			case "separator":
				builder.WordSeparator = NotNullIfNotNull(value);
				return;

			case "joint":
				builder.WordJoint = value; 
				return;

			case "typename":
				builder.TypeFormat = value.At(0) switch
				{
					'f' => WordTypeFormat.FullName,
					'a' => WordTypeFormat.Abbreviated,
					't' or 'c' => WordTypeFormat.TypeCode,
					_ => default
				}; 
				return;

			case "typeopen":
				builder.TypeOpen = NotNullIfNotNull(value);
				return;

			case "typejoint":
				builder.TypeSeparator = value;
				return;

			case "typeclose":
				builder.TypeClose = NotNullIfNotNull(value);
				return;

			case "omit":
				builder.OmitHeaders = value is "null" ? [] : value.Split(',').Select(x => x.Trim()).ToArray();
				return;

			default:
				return;
		}
	}

	[return: NotNullIfNotNull(nameof(str))]
	private static string? NotNullIfNotNull(string? str) => str is null or "null" ? null : str;

	public enum WordTypeFormat { FullName, Abbreviated, TypeCode }

	private sealed class ListLoaderBuilder
	{
		public string? ListOpen { private get; set; }

		public string? ListClose { private get; set; }

		public string? WordSeparator { private get; set; }

		public string WordJoint { private get; set; } = " ";

		public WordTypeFormat TypeFormat { private get; set; }

		public string? TypeOpen { private get; set; }

		public string TypeSeparator { private get; set; } = " ";

		public string? TypeClose { private get; set; }

		public string[] OmitHeaders { private get; set; } = [];

		public ListLoader Build() => new(ListOpen, ListClose, WordSeparator, WordJoint, TypeFormat, TypeOpen, TypeSeparator, TypeClose, OmitHeaders);
	}
}
