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

[DynamicLinqType]
public static class ProcHelper
{
	public static Func<T, bool> Pred<T>(this ProcCall proc)
		=> arg => (bool)proc.Invoke(new[] { arg })!;

	public static Func<T1, T2, bool> Pred2<T1, T2>(this ProcCall proc)
		=> (arg1, arg2) => (bool)proc.Invoke(new object?[] { arg1, arg2 })!;

	public static Func<T, TResult> Func<T, TResult>(this ProcCall proc)
		=> arg => (TResult)proc.Invoke(new[] { arg })!;

	public static Func<T1, T2, TResult> Func2<T1, T2, TResult>(this ProcCall proc)
		=> (arg1, arg2) => (TResult)proc.Invoke(new object?[] { arg1, arg2 })!;

	public static Func<T1, T2, T3, TResult> Func3<T1, T2, T3, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3) => (TResult)proc.Invoke(new object?[] { arg1, arg2, arg3 })!;
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
	public static object? Invoke(string op, dynamic x) => op switch
	{
		"+" => UnaryPlus(x),
		"-" => Negate(x),
		"!" => Not(x),
		"~" => Complement(x),
		"^" => Index(x),
		_ => throw new ArgumentException($"Invalid unary operator: {op}")
	};

	public static object? Invoke(string op, dynamic x, dynamic y) => op switch
	{
		"+" => Add(x, y),
		"-" => Subtract(x, y),
		"*" => Multiply(x, y),
		"/" => Divide(x, y),
		"%" => Modulo(x, y),
		"**" => Power(x, y),
		"==" => Equal(x, y),
		"!=" => NotEqual(x, y),
		"<" => LessThan(x, y),
		"<=" => LessThanOrEqual(x, y),
		">" => GreaterThan(x, y),
		">=" => GreaterThenOrEqual(x, y),
		"&" => And(x, y),
		"|" => Or(x, y),
		"^" => Xor(x, y),
		"&&" => AndAlso(x, y),
		"||" => OrElse(x, y),
		"<<" => LeftShift(x, y),
		">>" => RightShift(x, y),
		">>>" => UnsignedRightShift(x, y),
		"??" => Coalesce(x, y),
		".." => Range(x, y),
		_ => throw new ArgumentException($"Invalid binary operator: {op}")
	};

	public static object? Invoke(string op, dynamic x, dynamic y, dynamic z) => op switch
	{
		"?:" => Condition(x, y, z),
		_ => throw new ArgumentException($"Invalid ternary operator: {op}")
	};


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

	public static IEnumerable<(TSource Item, int Index)> WithIndex<TSource>(this IEnumerable<TSource> source)
		=> source.Select((x, i) => (x, i));

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

[DynamicLinqType]
public static class LinqExtension
{
	public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
		=> Enumerable.Aggregate(source, func);

	public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
		=> Enumerable.Aggregate(source, seed, func);

	public static TResult Aggregate<TSource, TAccumululate, TResult>(this IEnumerable<TSource> source, TAccumululate seed, Func<TAccumululate, TSource, TAccumululate> func, Func<TAccumululate, TResult> resultSelector)
		=> Enumerable.Aggregate(source, seed, func, resultSelector);

	public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Any(source, predicate);

	public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.All(source, predicate);

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Average(source, selector);

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Average(source, selector);

	public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Average(source, selector);

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Average(source, selector);

	public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Average(source, selector);

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Average(source, selector);

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Average(source, selector);

	public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Average(source, selector);

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Average(source, selector);

	public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Average(source, selector);

	public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Count(source, predicate);

	public static long LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.LongCount(source, predicate);

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.DistinctBy(source, keySelector);

	public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
		=> Enumerable.ExceptBy(first, second, keySelector);

	public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.First(source, predicate);

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.FirstOrDefault(source, predicate);

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
		=> Enumerable.FirstOrDefault(source, predicate, defaultValue);

	public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.GroupBy(source, keySelector);

	public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		=> Enumerable.GroupBy(source, keySelector, elementSelector);

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
		=> Enumerable.GroupBy(source, keySelector, resultSelector);

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
		=> Enumerable.GroupBy(source, keySelector, elementSelector, resultSelector);

	public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
		=> Enumerable.GroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);

	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
		=> Enumerable.IntersectBy(first, second, keySelector);

	public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
		=> Enumerable.Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector);

	public static TSource Last<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Last(source, predicate);

	public static TSource? LastOrDefault<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.LastOrDefault(source, predicate);

	public static TSource LastOrDefault<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
		=> Enumerable.LastOrDefault(source, predicate, defaultValue);

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ToLookup(source, keySelector);

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		=> Enumerable.ToLookup(source, keySelector, elementSelector);

	public static TSource? MaxBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.MaxBy(source, keySelector);

	public static int Max<TSource>(IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Max(source, selector);

	public static int? Max<TSource>(IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Max(source, selector);

	public static long Max<TSource>(IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Max(source, selector);

	public static long? Max<TSource>(IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Max(source, selector);

	public static float Max<TSource>(IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Max(source, selector);

	public static float? Max<TSource>(IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Max(source, selector);

	public static double Max<TSource>(IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Max(source, selector);

	public static double? Max<TSource>(IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Max(source, selector);

	public static decimal Max<TSource>(IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Max(source, selector);

	public static decimal? Max<TSource>(IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Max(source, selector);

	public static TResult? Max<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Max(source, selector);

	public static TSource? MinBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.MinBy(source, keySelector);

	public static int Min<TSource>(IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Min(source, selector);

	public static int? Min<TSource>(IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Min(source, selector);

	public static long Min<TSource>(IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Min(source, selector);

	public static long? Min<TSource>(IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Min(source, selector);

	public static float Min<TSource>(IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Min(source, selector);

	public static float? Min<TSource>(IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Min(source, selector);

	public static double Min<TSource>(IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Min(source, selector);

	public static double? Min<TSource>(IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Min(source, selector);

	public static decimal Min<TSource>(IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Min(source, selector);

	public static decimal? Min<TSource>(IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Min(source, selector);

	public static TResult? Min<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Min(source, selector);

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.OrderBy(source, keySelector);

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.OrderBy(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.OrderByDescending(source, keySelector);

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.OrderByDescending(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ThenBy(source, keySelector);

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.ThenBy(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ThenByDescending(source, keySelector);

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.ThenByDescending(source, keySelector, comparer);

	public static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Select(source, selector);

	public static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
		=> Enumerable.Select(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
		=> Enumerable.SelectMany(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
		=> Enumerable.SelectMany(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
		=> Enumerable.SelectMany(source, collectionSelector, resultSelector);

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
		=> Enumerable.SelectMany(source, collectionSelector, resultSelector);

	public static TSource Single<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Single(source, predicate);

	public static TSource? SingleOrDefault<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.SingleOrDefault(source, predicate);

	public static TSource SingleOrDefault<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
		=> Enumerable.SingleOrDefault(source, predicate, defaultValue);

	public static IEnumerable<TSource> SkipWhile<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.SkipWhile(source, predicate);

	public static IEnumerable<TSource> SkipWhile<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.SkipWhile(source, predicate);

	public static int Sum<TSource>(IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Sum(source, selector);

	public static long Sum<TSource>(IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Sum(source, selector);
	public static float Sum<TSource>(IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Sum(source, selector);

	public static double Sum<TSource>(IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Sum(source, selector);

	public static decimal Sum<TSource>(IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Sum(source, selector);

	public static int? Sum<TSource>(IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Sum(source, selector);

	public static long? Sum<TSource>(IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Sum(source, selector);

	public static float? Sum<TSource>(IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Sum(source, selector);

	public static double? Sum<TSource>(IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Sum(source, selector);

	public static decimal? Sum<TSource>(IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Sum(source, selector);

	public static IEnumerable<TSource> TakeWhile<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.TakeWhile(source, predicate);

	public static IEnumerable<TSource> TakeWhile<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.TakeWhile(source, predicate);

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		where TKey : notnull
		=> Enumerable.ToDictionary(source, keySelector);

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) 
		where TKey : notnull
		=> Enumerable.ToDictionary(source, keySelector, elementSelector);

	public static IEnumerable<TSource> UnionBy<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
		=> Enumerable.UnionBy(first, second, keySelector);

	public static IEnumerable<TSource> Where<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Where(source, predicate);

	public static IEnumerable<TSource> Where<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.Where(source, predicate);

	public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
		=> Enumerable.Zip(first, second, resultSelector);
}