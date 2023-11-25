using System.Linq.Dynamic.Core.CustomTypeProviders;
namespace SBFirstLast4.Dynamic.Extensions;

[DynamicLinqType]
public static class Reader
{
	public static Word[] ReadCsv(string csv)
	{
		var csvData = csv.Split('\n').Select(x => x.Trim()).ToArray();
		return csvData.Select(ReadWordText).ToArray();
	}

	public static Word ReadWordText(string text)
	{
		var wordRaw = text.Split();
		return new(wordRaw.At(0) ?? string.Empty, wordRaw.At(1)?.StringToType() ?? default, wordRaw.At(2)?.StringToType() ?? default);
	}
}
