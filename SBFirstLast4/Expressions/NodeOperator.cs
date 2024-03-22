namespace SBFirstLast4.Expressions;

public enum NodeOperator
{
	Head, And, Or, Xor, Nand, Nor, Xnor, Imply, Nimply
}

public enum EqualityOperator
{
	Equal, NotEqual
}

public enum ComparisonOperator
{
	Equal, NotEqual, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
}

public enum SpecializedCondition
{
	SingleTyped, DoubleTyped, Killable, Semikillable, Danger4, Fourxable
}

public static class OperatorExtension
{
	public static string String(bool b) => b ? string.Empty : "!";

	public static string String(NodeOperator op) => op switch
	{
		NodeOperator.And or NodeOperator.Or => $"@{op}",
		NodeOperator.Head => "@And",
		_ => op.ToString()
	};

	public static string String(EqualityOperator op) => op switch
	{
		EqualityOperator.Equal => "==",
		EqualityOperator.NotEqual => "!=",
		_ => throw new ArgumentException(null, nameof(op))
	};

	public static string String(ComparisonOperator op) => op switch
	{
		ComparisonOperator.Equal => "==",
		ComparisonOperator.NotEqual => "!=",
		ComparisonOperator.LessThan => "<",
		ComparisonOperator.LessThanOrEqual => "<=",
		ComparisonOperator.GreaterThan => ">",
		ComparisonOperator.GreaterThanOrEqual => ">=",
		_ => throw new ArgumentException(null, nameof(op))
	};

	public static string String(SpecializedCondition condition) => condition switch
	{
		SpecializedCondition.SingleTyped => nameof(Word.IsSingleType),
		SpecializedCondition.DoubleTyped => nameof(Word.IsDoubleType),
		SpecializedCondition.Killable => $"{nameof(Extensions.TreeSearchHelper.IsKillable)}()",
		SpecializedCondition.Semikillable => $"{nameof(Extensions.TreeSearchHelper.IsSemikillable)}()",
		SpecializedCondition.Danger4 => $"{nameof(Extensions.TreeSearchHelper.IsDanger4)}()",
		SpecializedCondition.Fourxable => $"{nameof(Extensions.TreeSearchHelper.Is4xable)}()",
		_ => throw new ArgumentException(null, nameof(condition))
	};

	public static string Space(bool b) => b ? string.Empty : " ";


	public static string ConditionToString(this SpecializedCondition condition) => condition switch
	{
		SpecializedCondition.SingleTyped => "単タイプ",
		SpecializedCondition.DoubleTyped => "複合タイプ",
		SpecializedCondition.Killable => "即死可能",
		SpecializedCondition.Semikillable => "準即死可能",
		SpecializedCondition.Danger4 => "４注単語",
		SpecializedCondition.Fourxable => "４倍可能",
		_ => "不明な条件"
	};

	public static NodeOperator SymbolToOperator(this string? str) => str switch
	{
		"&&" => NodeOperator.And,
		"||" => NodeOperator.Or,
		"^" => NodeOperator.Xor,
		"!&&" => NodeOperator.Nand,
		"!||" => NodeOperator.Nor,
		"!^" => NodeOperator.Xor,
		"→" => NodeOperator.Imply,
		"!→" => NodeOperator.Nimply,
		_ => NodeOperator.And
	};

	public static string ToSymbol(this NodeOperator op) => op switch
	{
		NodeOperator.And => "&&",
		NodeOperator.Or => "||",
		NodeOperator.Xor => "^",
		NodeOperator.Nand => "!&&",
		NodeOperator.Nor => "!||",
		NodeOperator.Xnor => "!^",
		NodeOperator.Imply => "→",
		NodeOperator.Nimply => "!→",
		_ => "??"
	};
}
