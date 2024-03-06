using SpawnDev.BlazorJS.JSObjects;
using System.Configuration;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using System.Text.RegularExpressions;
using IEnumerable = System.Collections.IEnumerable;

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

	public static T? At<T>(this IEnumerable<T> source, int index) => source.ElementAtOrDefault(index);

	public static T? At<T>(this T[] source, int index) => index < 0 || index >= source.Length ? default : source[index];

	public static T? At<T>(this IList<T> source, int index) => index < 0 || index >= source.Count ? default : source[index];

	public static TValue? At<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key) => source.TryGetValue(key, out var value) ? value : default;

	public static void Add<T>(this Stack<T> stack, T value) => stack.Push(value);

	public static void Add<T>(this Queue<T> queue, T value) => queue.Enqueue(value);


	/// <summary>
	/// SO dictionary for query source
	/// </summary>
	/// <seealso cref="ScriptExecutor._singletonEnumerable"/>
	public static int[] GetSingleton() => new[] { 0 };
}

#pragma warning disable IDE1006
[DynamicLinqType]
public static class @cast
{
	public static T @static<T>(object? obj) => (T)obj!;

	public static T? @dynamic<T>(object? obj) where T : class => obj as T;
}
#pragma warning restore

[DynamicLinqType]
public static class Operators
{
	public static object? Plus(dynamic x) => UnaryPlus(x);

	public static object? Plus(dynamic x, dynamic y) => Add(x, y);

	public static object? Minus(dynamic x) => Negate(x);

	public static object? Minus(dynamic x, dynamic y) => Subtract(x, y);

	public static object? UnaryPlus(dynamic x) => +x;

	public static object? Negate(dynamic x) => -x;

	public static object? NegateChecked(dynamic x) => checked(-x);

	public static object? Not(dynamic x) => !x;

	public static object? Complement(dynamic x) => ~x;

	public static object? Checked(dynamic x) => checked(x);

	public static object? Unchecked(dynamic x) => unchecked(x);

	public static double Power(dynamic x, dynamic y) => Math.Pow(x, y);

	public static object? Multiply(dynamic x, dynamic y) => x * y;

	public static object? MultiplyChecked(dynamic x, dynamic y) => checked(x * y);

	public static object? Divide(dynamic x, dynamic y) => x / y;

	public static object? Modulo(dynamic x, dynamic y) => x % y;

	public static object? Add(dynamic x, dynamic y) => x + y;

	public static object? AddChecked(dynamic x, dynamic y) => checked(x + y);

	public static object? Subtract(dynamic x, dynamic y) => x - y;

	public static object? SubtractChecked(dynamic x, dynamic y) => checked(x - y);

	public static object? LeftShift(dynamic x, dynamic y) => x << y;

	public static object? RightShift(dynamic x, dynamic y) => x >> y;

	public static T UnsignedRightShift<T>(T x, int b) where T : IShiftOperators<T, int, T> => x >>> b;

	public static TResult UnsignedRightShift<TLeft, TRight, TResult>(TLeft x, TRight b) where TLeft : IShiftOperators<TLeft, TRight, TResult> => x >>> b;

	public static object? Equal(dynamic x, dynamic y) => x == y;

	public static object? NotEqual(dynamic x, dynamic y) => x != y;

	public static object? LessThan(dynamic x, dynamic y) => x < y;

	public static object? LessThanOrEqual(dynamic x, dynamic y) => x <= y;

	public static object? GreaterThan(dynamic x, dynamic y) => x > y;

	public static object? GreaterThenOrEqual(dynamic x, dynamic y) => x >= y;

	public static object? And(dynamic x, dynamic y) => x & y;

	public static object? Or(dynamic x, dynamic y) => x | y;

	public static object? Xor(dynamic x, dynamic y) => x ^ y;

	public static object? AndAlso(dynamic x, dynamic y) => x && y;

	public static object? OrElse(dynamic x, dynamic y) => x || y;

	public static object? Coalesce(dynamic x, dynamic y) => x ?? y;

	public static object? Condition(dynamic x, dynamic y, dynamic z) => x ? y : z;

	public static T? Default<T>() => default;

	public static Type TypeOf<T>() => typeof(T);

	public static Index Index(int x) => ^x;

	public static Range Range() => ..;

	public static Range Range(int start) => start..;

	public static Range Range(Index start) => start..;

	public static Range Range(int start, int end) => start..end;

	public static Range Range(Index start, int end) => start..end;

	public static Range Range(int start, Index end) => start..end;

	public static Range Range(Index start, Index end) => start..end;

	public static Range RangeEnd(int end) => ..end;

	public static Range RangeEnd(Index end) => ..end;
}

[DynamicLinqType]
public static class Linq
{
	public static bool IsLinqDefined => CustomTypeProvider.IsLinqDefined;

	public static IEnumerable<TSource> Flatten<TSource>(this IEnumerable<IEnumerable<TSource>> source)
		=> source.SelectMany(x => x);

	public static IEnumerable<TSource> Sort<TSource>(this IEnumerable<TSource> source)
		=> source.Order();

	public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
		=> source.OrderBy(_ => Guid.NewGuid());

	public static string StringJoin<TSource>(this IEnumerable<TSource> source, char separator)
		=> string.Join(separator, source);

	public static string StringJoin<TSource>(this IEnumerable<TSource> source, string separator)
		=> string.Join(separator, source);

	public static string StringJoin<TSource>(this IEnumerable<TSource> source)
		=> string.Join(string.Empty, source);

	public static bool Any<TSource>(this IEnumerable<TSource> source) 
		=> Enumerable.Any(source);

	public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element) 
		=> Enumerable.Append(source, element);

	public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
		=> Enumerable.Prepend(source, element);

	public static double Average(this IEnumerable<int> source)
		=> Enumerable.Average(source);

	public static double Average(this IEnumerable<long> source)
		=> Enumerable.Average(source);

	public static float Average(this IEnumerable<float> source)
		=> Enumerable.Average(source);

	public static double Average(this IEnumerable<double> source)
		=> Enumerable.Average(source);

	public static decimal Average(this IEnumerable<decimal> source)
		=> Enumerable.Average(source);

	public static double? Average(this IEnumerable<int?> source)
		=> Enumerable.Average(source);

	public static double? Average(this IEnumerable<long?> source)
		=> Enumerable.Average(source);

	public static float? Average(this IEnumerable<float?> source)
		=> Enumerable.Average(source);

	public static double? Average(this IEnumerable<double?> source)
		=> Enumerable.Average(source);

	public static decimal? Average(this IEnumerable<decimal?> source)
		=> Enumerable.Average(source);

	public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
		=> Enumerable.OfType<TResult>(source);

	public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source) 
		=> Enumerable.Cast<TResult>(source);

	public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
		=> Enumerable.Chunk(source, size);

	public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
		=> Enumerable.Concat(first, second);

	public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
		=> Enumerable.Contains(source, value);

	public static int Count<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Count(source);

	public static bool TryGetNonEnumeratedCound<TSource>(this IEnumerable<TSource> source, out int count)
		=> Enumerable.TryGetNonEnumeratedCount(source, out count);

	public static long LongCount<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.LongCount(source);

	public static IEnumerable<TSource?> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.DefaultIfEmpty(source);

	public static IEnumerable<TSource?> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
		=> Enumerable.DefaultIfEmpty(source, defaultValue);

	public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Distinct(source);

	public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
		=> Enumerable.ElementAt(source, index);

	public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
		=> Enumerable.ElementAtOrDefault(source, index);

	public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.AsEnumerable(source);

	public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
		=> Enumerable.Except(first, second);

	public static TSource First<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.First(source);

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.FirstOrDefault(source);

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
		=> Enumerable.FirstOrDefault(source, defaultValue);

	public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
		=> Enumerable.Intersect(first, second);

	public static TSource Last<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Last(source);

	public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.LastOrDefault(source);

	public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
		=> Enumerable.LastOrDefault(source, defaultValue);

	public static int Max(this IEnumerable<int> source)
		=> Enumerable.Max(source);

	public static int? Max(this IEnumerable<int?> source)
		=> Enumerable.Max(source);

	public static long Max(this	IEnumerable<long> source)
		=> Enumerable.Max(source);

	public static long? Max(this IEnumerable<long?> source)
		=> Enumerable.Max(source);

	public static double Max(this IEnumerable<double> source)
		=> Enumerable.Max(source);

	public static double? Max(this IEnumerable<double?> source)
		=> Enumerable.Max(source);

	public static float Max(this IEnumerable<float> source)
		=> Enumerable.Max(source);

	public static float? Max(this IEnumerable<float?> source)
		=> Enumerable.Max(source);

	public static decimal Max(this IEnumerable<decimal> source)
		=> Enumerable.Max(source);

	public static decimal? Max(this IEnumerable<decimal?> source)
		=> Enumerable.Max(source);

	public static TSource? Max<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Max(source);

	public static int Min(this IEnumerable<int> source)
		=> Enumerable.Min(source);

	public static long Min(this IEnumerable<long> source)
		=> Enumerable.Min(source);

	public static int? Min(this IEnumerable<int?> source)
		=> Enumerable.Min(source);

	public static long? Min(this IEnumerable<long?> source)
		=> Enumerable.Min(source);

	public static float Min(this IEnumerable<float> source)
	=> Enumerable.Min(source);

	public static float? Min(this IEnumerable<float?> source)
		=> Enumerable.Min(source);

	public static double Min(this IEnumerable<double> source)
		=> Enumerable.Min(source);

	public static double? Min(this IEnumerable<double?> source)
		=> Enumerable.Min(source);

	public static decimal Min(this IEnumerable<decimal> source)
		=> Enumerable.Min(source);

	public static decimal? Min(this IEnumerable<decimal?> source)
		=> Enumerable.Min(source);

	public static TSource? Min<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Min(source);

	public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source)
		=> Enumerable.Order(source);

	public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source)
		=> Enumerable.OrderDescending(source);

	public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Reverse(source);

	public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
		=> Enumerable.SequenceEqual(first, second);

	public static TSource Single<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.Single(source);

	public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.SingleOrDefault(source);

	public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
		=> Enumerable.SingleOrDefault(source, defaultValue);

	public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
		=> Enumerable.Skip(source, count);

	public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int cound)
		=> Enumerable.SkipLast(source, cound);

	public static int Sum(this IEnumerable<int> source)
		=> Enumerable.Sum(source);

	public static long Sum(this IEnumerable<long> source)
		=> Enumerable.Sum(source);

	public static float Sum(this IEnumerable<float> source)
		=> Enumerable.Sum(source);

	public static double Sum(this IEnumerable<double> source)
		=> Enumerable.Sum(source);

	public static decimal Sum(this IEnumerable<decimal> source)
		=> Enumerable.Sum(source);

	public static int? Sum(this IEnumerable<int?> source)
		=> Enumerable.Sum(source);

	public static long? Sum(this IEnumerable<long?> source)
		=> Enumerable.Sum(source);

	public static float? Sum(this IEnumerable<float?> source)
		=> Enumerable.Sum(source);

	public static double? Sum(this IEnumerable<double?> source)
		=> Enumerable.Sum(source);

	public static decimal? Sum(this IEnumerable<decimal?> source)
		=> Enumerable.Sum(source);

	public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
		=> Enumerable.Take(source, count);

	public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Range range)
		=> Enumerable.Take(source, range);

	public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
		=> Enumerable.TakeLast(source, count);

	public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.ToArray(source);

	public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.ToList(source);

	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
		=> Enumerable.ToHashSet(source);

	public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first,  IEnumerable<TSource> second)
		=> Enumerable.Union(first, second);

	public static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
		=> Enumerable.Zip(first, second);

	public static IEnumerable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
		=> Enumerable.Zip(first, second, third);
}