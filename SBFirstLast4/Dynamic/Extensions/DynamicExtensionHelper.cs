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

	/// <summary>
	/// SO dictionary for query source
	/// </summary>
	/// <seealso cref="ScriptExecutor._singletonEnumerable"/>
	public static int[] GetSingleton() => new[] { 0 };
}

[DynamicLinqType]
public static class List
{
	public static string Add(dynamic? @this, dynamic? item)
	{
		@this?.Add(item);
		return string.Empty;
	}

	public static string AddRange(dynamic? @this, dynamic? collection)
	{
		@this?.AddRange(collection);
		return string.Empty;
	}

	public static int BinarySearch(dynamic @this, dynamic? item)
		=> @this.BinarySearch(item);

	public static string Clear(dynamic @this)
	{
		@this?.Clear();
		return string.Empty;
	}

	public static bool Contains(dynamic @this, dynamic? item) => @this.Contains(item);

	public static string CopyTo(dynamic @this, dynamic? array)
	{
		@this.CopyTo(array);
		return string.Empty;
	}

	public static string CopyTo(dynamic @this, dynamic? array, int arrayIndex)
	{
		@this.CopyTo(array, arrayIndex);
		return string.Empty;
	}

	public static int EnsureCapacity(dynamic @this, int capacity) => @this.EnsureCapacity(capacity);

	public static int IndexOf(dynamic @this, dynamic? item) => @this.IndexOf(item);

	public static int IndexOf(dynamic @this, dynamic? item, int index) => @this.IndexOf(item, index);

	public static int IndexOf(dynamic @this, dynamic? item, int index, int count) => @this.IndexOf(item, index, count);

	public static string Insert(dynamic @this, int index, dynamic? item)
	{
		@this?.Insert(index, item);
		return string.Empty;
	}

	public static string InsertRange(dynamic @this, int index, dynamic? collection)
	{
		@this.InsertRange(index, collection);
		return string.Empty;
	}

	public static int LastIndexOf(dynamic @this, dynamic? item) => @this.LastIndexOf(item);

	public static int LastIndexOf(dynamic @this, dynamic? item, int index) => @this.LastIndexOf(item, index);

	public static int LastIndexOf(dynamic @this, dynamic? item, int index, int count) => @this.LastIndexOf(item, index, count);

	public static bool Remove(dynamic @this, dynamic? item) => @this.Remove(item);

	public static string RemoveAt(dynamic @this, int index)
	{
		@this.RemoveAt(index);
		return string.Empty;
	}

	public static string RemoveRange(dynamic @this, int index, int count)
	{
		@this?.RemoveRange(index, count);
		return string.Empty;
	}

	public static string Reverse(dynamic @this)
	{
		@this.Reverse();
		return string.Empty;
	}

	public static string Reverse(dynamic @this, int index, int count)
	{
		@this.Reverse(index, count);
		return string.Empty;
	}

	public static string Sort(dynamic @this)
	{
		@this.Sort();
		return string.Empty;
	}

	public static string TrimExcess(dynamic @this)
	{
		@this.TrimExcess();
		return string.Empty;
	}
}

[DynamicLinqType]
public static class Hash
{
	public static string Add(dynamic @this, dynamic? key, dynamic? value)
	{
		@this.Add(key, value);
		return string.Empty;
	}

	public static string Clear(dynamic @this)
	{
		@this.Clear();
		return string.Empty;
	}

	public static bool ContainsKey(dynamic @this, dynamic? key) => @this.ContainsKey(key);

	public static bool ContainsValue(dynamic @this, dynamic? value) => @this.ContainsValue(value);

	public static int EnsureCapacity(dynamic @this, int capacity) => @this.EnsureCapacity(capacity);

	public static bool Remove(dynamic @this, dynamic? key) => @this.Remove(key);

	public static bool Remove(dynamic @this, dynamic? key, dynamic? value) => @this.Remove(key, value);

	public static string TrimExcess(dynamic @this, int capacity)
	{
		@this.TrimExcess(capacity);
		return string.Empty;
	}

	public static bool TryAdd(dynamic @this, dynamic? key, dynamic? value) => @this.TryAdd(key, value);

	public static bool TryGetValue(dynamic @this, dynamic? key, out dynamic? value) => @this.TryGetValue(key, out value);
}