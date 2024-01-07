using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Dynamic.Extensions;

[DynamicLinqType]
public static class DynamicExtensionHelper
{
	public static char FirstChar(this string str) => str.At(0);

	public static char LastChar(this string str) => str.GetLastChar();

	public static Word ToWord(this string name) => new(name, WordType.Empty, WordType.Empty);

	public static Word ToWord(this string name, WordType type1) => new(name, type1, WordType.Empty);

	public static Word ToWord(this string name, WordType type1, WordType type2) => new(name, type1, type2);

	public static Word Deduce(this string name) => Word.FromString(name);

	public static Regex ToRegex(this string pattern) => new(pattern);

	public static Regex ToRegex(this string pattern, RegexOptions options) => new(pattern, options);

	public static Regex ToRegex(this string pattern, RegexOptions options, TimeSpan matchTimeout) => new(pattern, options, matchTimeout);

	public static char At(this string source, int index) => index < 0 || index >= source.Length ? default : source[index];

	public static char At(this string source, Index index) => index.Value < 0 || index.Value >= source.Length ? default : source[index];

	public static int[] GetSingleton() => new[] { 0 };
}
