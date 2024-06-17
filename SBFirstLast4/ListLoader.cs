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
public sealed class ListLoader(string listOpen, string listClose, string wordSeparator, string wordJoint, WordTypeFormat typeFormat, string typeOpen, string typeSeparator, string typeClose, string[] omitHeaders)
{
	private readonly string _listOpen = listOpen;

	private readonly string _listClose = listClose;

	private readonly string _wordSeparator = wordSeparator;

	private readonly string _wordJoint = wordJoint;

	private readonly WordTypeFormat _typeFormat = typeFormat;

	private readonly string _typeOpen = typeOpen;

	private readonly string _typeSeparator = typeSeparator;

	private readonly string _typeClose = typeClose;

	private readonly string[] _omitHeaders = omitHeaders;

	public Word[] Load(string input)
	{
		var source = input;
		if (!string.IsNullOrEmpty(_listOpen))
		{
			var index = input.IndexOf(_listOpen);
			if(index == -1)
				return [];
			source = input[(index + _listOpen.Length)..];
		}
		if (!string.IsNullOrEmpty(_listClose))
		{
			var index = input.IndexOf(_listClose);
			if(index == -1) 
				return [];
			source = input[..index];
		}

		var result = new List<Word>();
		foreach(var i in source.Split(_wordSeparator))
		{
			var joint = i.IndexOf(_wordJoint);
			var wordName = i[..joint];
			var strTypes = i[(joint + 1)..];
			var typeOpen = 0;
			if (_typeOpen.Length > 0)
				typeOpen = strTypes.IndexOf(_typeOpen);

		}
		return [..result];
	}

	public enum WordTypeFormat { FullName, Abbreviated, TypeCode }
}
