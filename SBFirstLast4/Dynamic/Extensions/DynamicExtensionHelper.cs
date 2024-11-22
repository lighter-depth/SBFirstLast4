using Antlr4.Runtime.Tree.Pattern;
using System.Globalization;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

	public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, (TKey Key, TValue Value) tuple)
		where TKey : notnull => dictionary.Add(tuple.Key, tuple.Value);

	/// <summary>
	/// SO dictionary for query source
	/// </summary>
	/// <seealso cref="ScriptExecutor._singletonEnumerable"/>
	public static int[] GetSingleton() => [0];

	public static string HashTest(string str)
	{
		var hashRoot = Encoding.UTF8.GetBytes(str);

		var hash = System.Security.Cryptography.SHA256.HashData(hashRoot);

		if (hash is null) return "UNKNOWN_HASH";

		return hash.Select(b => b.ToString("x2")).StringJoin();
	}

	private static Word[] PittanWords => _pittanWords ??= Words.TypedWords.Where(x => x.Length > 1).ToArray();
	private static Word[]? _pittanWords;

	// #define PITTAN(_X) DynamicExtensionHelper.Pittan0("_X")
	public static MultiWord Pittan0(string name)
	{
		if (name.Length < 2)
			return (MultiWord)Word.FromString(name);

		var buffer = new List<WordType>();
		var words = new List<Word>();

		foreach (var i in PittanWords)
			if (name.Contains(i.Name))
			{
				buffer.AddRange(i.Types);
				words.Add(i);
			}
		foreach (var i in words)
		{
			var count = (name.Length - name.Replace(i.Name, string.Empty).Length) / i.Length;
			for (var j = 0; j < count - 1; j++)
				buffer.AddRange(i.Types);
		}
		return new MultiWord(name, buffer.ToArray());
	}
}

[DynamicLinqType]
public sealed class Disposable(ProcCall proc) : IDisposable, IAsyncDisposable
{
	private readonly AsyncProcCall _proc = proc.Async();

	public void Dispose() => DisposeAsync().AsTask();

	public async ValueTask DisposeAsync() => await _proc.Invoke(Array.Empty<object>());
}

[DynamicLinqType]
public static class ProcHelper
{
	public static Func<string> Action(this ProcCall proc)
		=> () => proc.InvokeVoid([]);

	public static Func<T, string> Action<T>(this ProcCall proc)
		=> arg => proc.InvokeVoid([arg]);

	public static Func<T1, T2, string> Action<T1, T2>(this ProcCall proc)
		=> (arg1, arg2) => proc.InvokeVoid([arg1, arg2]);

	public static Func<T1, T2, T3, string> Action<T1, T2, T3>(this ProcCall proc)
		=> (arg1, arg2, arg3) => proc.InvokeVoid([arg1, arg2, arg3]);

	public static Func<T1, T2, T3, T4, string> Action<T1, T2, T3, T4>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4) => proc.InvokeVoid([arg1, arg2, arg3, arg4]);

	public static Func<T1, T2, T3, T4, T5, string> Action<T1, T2, T3, T4, T5>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5) => proc.InvokeVoid([arg1, arg2, arg3, arg4, arg5]);

	public static Func<T1, T2, T3, T4, T5, T6, string> Action<T1, T2, T3, T4, T5, T6>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6) => proc.InvokeVoid([arg1, arg2, arg3, arg4, arg5, arg6]);

	public static Func<T1, T2, T3, T4, T5, T6, T7, string> Action<T1, T2, T3, T4, T5, T6, T7>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => proc.InvokeVoid([arg1, arg2, arg3, arg4, arg5, arg6, arg7]);

	public static Func<T1, T2, T3, T4, T5, T6, T7, T8, string> Action<T1, T2, T3, T4, T5, T6, T7, T8>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => proc.InvokeVoid([arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8]);

	public static Func<bool> Predicate(this ProcCall proc)
		=> () => (bool)proc.Invoke([])!;

	public static Func<T, bool> Predicate<T>(this ProcCall proc)
		=> arg => (bool)proc.Invoke([arg])!;

	public static Func<T1, T2, bool> Predicate<T1, T2>(this ProcCall proc)
		=> (arg1, arg2) => (bool)proc.Invoke([arg1, arg2])!;

	public static Func<T1, T2, T3, bool> Predicate<T1, T2, T3>(this ProcCall proc)
		=> (arg1, arg2, arg3) => (bool)proc.Invoke([arg1, arg2, arg3])!;

	public static Func<TResult> Func<TResult>(this ProcCall proc)
		=> () => (TResult)proc.Invoke([])!;

	public static Func<T, TResult> Func<T, TResult>(this ProcCall proc)
		=> arg => (TResult)proc.Invoke([arg])!;

	public static Func<T1, T2, TResult> Func<T1, T2, TResult>(this ProcCall proc)
		=> (arg1, arg2) => (TResult)proc.Invoke([arg1, arg2])!;

	public static Func<T1, T2, T3, TResult> Func<T1, T2, T3, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3) => (TResult)proc.Invoke([arg1, arg2, arg3])!;

	public static Func<T1, T2, T3, T4, TResult> Func<T1, T2, T3, T4, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4) => (TResult)proc.Invoke([arg1, arg2, arg3, arg4])!;

	public static Func<T1, T2, T3, T4, T5, TResult> Func<T1, T2, T3, T4, T5, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5) => (TResult)proc.Invoke([arg1, arg2, arg3, arg4, arg5])!;

	public static Func<T1, T2, T3, T4, T5, T6, TResult> Func<T1, T2, T3, T4, T5, T6, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6) => (TResult)proc.Invoke([arg1, arg2, arg3, arg4, arg5, arg6])!;

	public static Func<T1, T2, T3, T4, T5, T6, T7, TResult> Func<T1, T2, T3, T4, T5, T6, T7, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => (TResult)proc.Invoke([arg1, arg2, arg3, arg4, arg5, arg6, arg7])!;

	public static Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this ProcCall proc)
		=> (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => (TResult)proc.Invoke([arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8])!;

}

#pragma warning disable IDE1006

[DynamicLinqType]
public static class @cast
{
	public static T @static<T>(dynamic obj) => (T)obj;

	public static T? @dynamic<T>(object? obj) where T : class => obj as T;

	public static T? @convert<T>(object? obj) => (T?)((IConvertible?)obj)?.ToType(typeof(T), CultureInfo.InvariantCulture);

	public static TTo? @reinterpret<TFrom, TTo>(TFrom obj) => ReinterpretImpl<TFrom, TTo>(obj);

	public static TTo[] @marshal<TFrom, TTo>(TFrom[] arr)
		where TFrom: struct where TTo: struct
		=> MemoryMarshal.Cast<TFrom, TTo>(arr.AsSpan()).ToArray();

	private static unsafe TTo? ReinterpretImpl<TFrom, TTo>(TFrom obj)
	{
		static nint id(nint n) => n;
		var cast = (delegate*<ref TFrom, ref TTo>)(delegate*<nint, nint>)&id;
		var result = cast(ref obj);
		return result;
	}
}

[DynamicLinqType]
public static class @operator
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
		"===" => StrictEqual(x, y),
		"!==" => StrictNotEqual(x, y),
		"<" => LessThan(x, y),
		"<=" => LessThanOrEqual(x, y),
		">" => GreaterThan(x, y),
		">=" => GreaterThenOrEqual(x, y),
		"<=>" => Spaceship(x, y),
		"&" => And(x, y),
		"|" => Or(x, y),
		"^" => Xor(x, y),
		"&&" => AndAlso(x, y),
		"||" => OrElse(x, y),
		"<<" => LeftShift(x, y),
		">>" => RightShift(x, y),
		">>>" => UnsignedRightShiftImpl(x, y),
		"??" => Coalesce(x, y),
		"in" => In(x, y),
		"=~" => Match(x, y),
		"!~" => NotMatch(x, y),
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

	public static Index Hat(dynamic x) => Index(x);

	public static object? Hat(dynamic x, dynamic y) => Xor(x, y);


	public static object? UnaryPlus(dynamic x) => +x;

	public static object? Negate(dynamic x) => -x;

	public static object? NegateChecked(dynamic x) => checked(-x);

	public static object? Not(dynamic x) => !x;

	public static object? Complement(dynamic x) => ~x;

	public static object? Checked(dynamic x) => checked(x);

	public static object? Unchecked(dynamic x) => unchecked(x);

	public static async Task<string> Await(Task task)
	{
		await task;
		return string.Empty;
	}

	public static async Task<T> Await<T>(Task<T> task) => await task;

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

	public static object? UnsignedRightShift(dynamic x, dynamic y) => UnsignedRightShiftImpl(x, y);

	private static T UnsignedRightShiftImpl<T>(T x, int b) where T : IShiftOperators<T, int, T> => x >>> b;

	public static object? Equal(dynamic x, dynamic y) => x == y;

	public static object? NotEqual(dynamic x, dynamic y) => x != y;

	public static bool StrictEqual(dynamic x, dynamic y) => ReferenceEquals(x, y);

	public static bool StrictNotEqual(dynamic x, dynamic y) => !ReferenceEquals(x, y);

	public static object? LessThan(dynamic x, dynamic y) => x < y;

	public static object? LessThanOrEqual(dynamic x, dynamic y) => x <= y;

	public static object? GreaterThan(dynamic x, dynamic y) => x > y;

	public static object? GreaterThenOrEqual(dynamic x, dynamic y) => x >= y;

	public static int Spaceship(dynamic x, dynamic y) => x == y ? 0 : x < y ? -1 : 1;

	public static object? And(dynamic x, dynamic y) => x & y;

	public static object? Or(dynamic x, dynamic y) => x | y;

	public static object? Xor(dynamic x, dynamic y) => x ^ y;

	public static object? AndAlso(dynamic x, dynamic y) => x && y;

	public static object? OrElse(dynamic x, dynamic y) => x || y;

	public static object? Coalesce(dynamic x, dynamic y) => x ?? y;

	public static bool In(dynamic x, dynamic y) => Enumerable.Contains(y, x);

	public static bool Match(Regex x, string y) => x.IsMatch(y);

	public static bool NotMatch(Regex x, string y) => !x.IsMatch(y);

	public static object? Condition(dynamic x, dynamic y, dynamic z) => x ? y : z;

	public static bool Is<T>(object? obj) => obj is T;

	public static object? New<T>(params object?[] args) => Activator.CreateInstance(typeof(T), args: args);

	public static T? Default<T>() => default;

	public static Type TypeOf<T>() => typeof(T);

	public static int SizeOf<T>() => Marshal.SizeOf<T>();

	public static int ArrayLength(dynamic obj)
	{
		if (obj is Array arr)
			return arr.Length;

		if (obj is System.Collections.IList list)
			return list.Count;

		if (obj is string str)
			return str.Length;

		if (obj is IEnumerable enumerable)
		{
			var typedEnumerable = enumerable.OfType<object>();
			return typedEnumerable.TryGetNonEnumeratedCount(out var count) ? count : typedEnumerable.Count();
		}
		return default;
	}

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

	public static object?[] Args(params object?[] args) => args;
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

	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
		where TKey : notnull => source.ToDictionary(kv => kv.Key, kv => kv.Value);

	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey, TValue)> source)
		where TKey : notnull => source.ToDictionary(t => t.Item1, t => t.Item2);

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

	public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
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

	public static long Max(this IEnumerable<long> source)
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

	public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
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

	public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.GroupBy(source, keySelector);

	public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		=> Enumerable.GroupBy(source, keySelector, elementSelector);

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
		=> Enumerable.GroupBy(source, keySelector, resultSelector);

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
		=> Enumerable.GroupBy(source, keySelector, elementSelector, resultSelector);

	public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
		=> Enumerable.GroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);

	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
		=> Enumerable.IntersectBy(first, second, keySelector);

	public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
		=> Enumerable.Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector);

	public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Last(source, predicate);

	public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.LastOrDefault(source, predicate);

	public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
		=> Enumerable.LastOrDefault(source, predicate, defaultValue);

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ToLookup(source, keySelector);

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		=> Enumerable.ToLookup(source, keySelector, elementSelector);

	public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.MaxBy(source, keySelector);

	public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Max(source, selector);

	public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Max(source, selector);

	public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Max(source, selector);

	public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Max(source, selector);

	public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Max(source, selector);

	public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Max(source, selector);

	public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Max(source, selector);

	public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Max(source, selector);

	public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Max(source, selector);

	public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Max(source, selector);

	public static TResult? Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Max(source, selector);

	public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.MinBy(source, keySelector);

	public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Min(source, selector);

	public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Min(source, selector);

	public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Min(source, selector);

	public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Min(source, selector);

	public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Min(source, selector);

	public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Min(source, selector);

	public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Min(source, selector);

	public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Min(source, selector);

	public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Min(source, selector);

	public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Min(source, selector);

	public static TResult? Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Min(source, selector);

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.OrderBy(source, keySelector);

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.OrderBy(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.OrderByDescending(source, keySelector);

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.OrderByDescending(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ThenBy(source, keySelector);

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.ThenBy(source, keySelector, comparer);

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		=> Enumerable.ThenByDescending(source, keySelector);

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
		=> Enumerable.ThenByDescending(source, keySelector, comparer);

	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
		=> Enumerable.Select(source, selector);

	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
		=> Enumerable.Select(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
		=> Enumerable.SelectMany(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
		=> Enumerable.SelectMany(source, selector);

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
		=> Enumerable.SelectMany(source, collectionSelector, resultSelector);

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
		=> Enumerable.SelectMany(source, collectionSelector, resultSelector);

	public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Single(source, predicate);

	public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.SingleOrDefault(source, predicate);

	public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
		=> Enumerable.SingleOrDefault(source, predicate, defaultValue);

	public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.SkipWhile(source, predicate);

	public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.SkipWhile(source, predicate);

	public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
		=> Enumerable.Sum(source, selector);

	public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
		=> Enumerable.Sum(source, selector);
	public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
		=> Enumerable.Sum(source, selector);

	public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
		=> Enumerable.Sum(source, selector);

	public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
		=> Enumerable.Sum(source, selector);

	public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
		=> Enumerable.Sum(source, selector);

	public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
		=> Enumerable.Sum(source, selector);

	public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
		=> Enumerable.Sum(source, selector);

	public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
		=> Enumerable.Sum(source, selector);

	public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
		=> Enumerable.Sum(source, selector);

	public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.TakeWhile(source, predicate);

	public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.TakeWhile(source, predicate);

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		where TKey : notnull
		=> Enumerable.ToDictionary(source, keySelector);

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
		where TKey : notnull
		=> Enumerable.ToDictionary(source, keySelector, elementSelector);

	public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
		=> Enumerable.UnionBy(first, second, keySelector);

	public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		=> Enumerable.Where(source, predicate);

	public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		=> Enumerable.Where(source, predicate);

	public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
		=> Enumerable.Zip(first, second, resultSelector);
}