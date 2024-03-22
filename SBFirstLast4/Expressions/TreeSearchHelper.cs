using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Expressions.Extensions;

#pragma warning disable IDE1006

[DynamicLinqType]
public static class TreeSearchHelper
{
	public static char FirstChar(this Word word) => word.Start;

	public static char LastChar(this Word word) => word.End;

	public static bool IsKillable(this Word word) => AuxLists.Killable.Contains(word.Name);

	public static bool IsSemikillable(this Word word) => AuxLists.SemiKillable.Contains(word.Name);

	public static bool IsDanger4(this Word word) => AuxLists.Danger4.Contains(word.Name);

	public static bool Is4xable(this Word word) => AuxLists.CanBe4xed.Contains(word.Name);

	public static bool __Regex_IsMatch__(this string str, string pattern) => Regex.IsMatch(str, pattern);

	public static bool __Regex_IsMatch__(this Word word, string pattern) => Regex.IsMatch(word.Name, pattern);
}

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
