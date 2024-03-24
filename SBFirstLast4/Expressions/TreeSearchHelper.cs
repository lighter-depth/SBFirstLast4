using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Expressions.Extensions;

#pragma warning disable IDE1006

[DynamicLinqType]
public static class TreeSearchHelper
{
	public static char FirstChar(this Word word) => word.Start;

	public static char LastChar(this Word word) => word.End;

	public static bool IsKillable(this Word word) => Array.BinarySearch(AuxLists.Killable, word.Name) > -1;

	public static bool IsSemikillable(this Word word) => Array.BinarySearch(AuxLists.SemiKillable, word.Name) > -1;

	public static bool IsDanger4(this Word word) => Array.BinarySearch(AuxLists.Danger4, word.Name) > -1;

	public static bool Is4xable(this Word word) => Array.BinarySearch(AuxLists.CanBe4xed, word.Name) > -1;

	public static bool __Regex_IsMatch__(this string str, string pattern) => Regex.IsMatch(str, pattern);

	public static bool __Regex_IsMatch__(this Word word, string pattern) => Regex.IsMatch(word.Name, pattern);
}

#pragma warning restore

[DynamicLinqType]
public class BooleanEvaluator
{
	private bool _value;

	public BooleanEvaluator() => _value = true;

	public static BooleanEvaluator Create() => new();

	public bool Evaluate() => _value;

	public BooleanEvaluator And(bool expr)
	{
		_value = BooleanOperators.And(_value, expr);
		return this;
	}

	public BooleanEvaluator Or(bool expr)
	{
		_value = BooleanOperators.Or(_value, expr);
		return this;
	}

	public BooleanEvaluator Xor(bool expr)
	{
		_value = BooleanOperators.Xor(_value, expr);
		return this;
	}

	public BooleanEvaluator Nand(bool expr)
	{
		_value = BooleanOperators.Nand(_value, expr);
		return this;
	}

	public BooleanEvaluator Nor(bool expr)
	{
		_value = BooleanOperators.Nor(_value, expr);
		return this;
	}

	public BooleanEvaluator Xnor(bool expr)
	{
		_value = BooleanOperators.Xnor(_value, expr);
		return this;
	}

	public BooleanEvaluator Imply(bool expr)
	{
		_value = BooleanOperators.Imply(_value, expr);
		return this;
	}

	public BooleanEvaluator Nimply(bool expr)
	{
		_value = BooleanOperators.Nimply(_value, expr);
		return this;
	}

	public static implicit operator bool(BooleanEvaluator b) => b._value;
}

[DynamicLinqType]
public static class BooleanOperators
{
	public static bool And(bool x, bool y) => x && y;

	public static bool Or(bool x, bool y) => x || y;

	public static bool Xor(bool x, bool y) => x ^ y;

	public static bool Nand(bool x, bool y) => !And(x, y);

	public static bool Nor(bool x, bool y) => !Or(x, y);

	public static bool Xnor(bool x, bool y) => !Xor(x, y);

	public static bool Imply(bool x, bool y) => !x || y;

	public static bool Nimply(bool x, bool y) => !Imply(x, y);
}
