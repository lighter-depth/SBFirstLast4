namespace SBFirstLast4.Expressions;

public enum NodeOperator
{
	Head, And, AndNot, Or, OrNot
}

public enum EqualityOperator
{
	Equal, NotEqual
}

public enum ComparisonOperator
{
	Equal, NotEqual, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
}
public static class OperatorExtension
{
	public static string String(NodeOperator op) => op switch
	{
		NodeOperator.And => "&&",
		NodeOperator.AndNot => "&& !",
		NodeOperator.Or => "||",
		NodeOperator.OrNot => "|| !",
		_ => string.Empty
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
}
