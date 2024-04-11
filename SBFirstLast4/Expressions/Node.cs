using SBFirstLast4.Syntax;
using static SBFirstLast4.Expressions.OperatorExtension;

namespace SBFirstLast4.Expressions;

public class Node
{
	public int Id { get; }

	public NodeOperator Operator { get; internal set; }

	private static int IdGenerator = 0;

	public bool IsTypedOnly => this is TypeNode or SpecializedNode;

	public Node(NodeOperator op) => (Operator, Id) = (op, IdGenerator++);
}

public sealed class FirstNode : Node
{
	public char First { get; internal set; }

	public EqualityOperator Equality { get; internal set; }

	public string Description => $"{String(Equality == default)}{First} =>";

	public string ShortDescription => $"{String(Equality == default)}{First}";

	public FirstNode(NodeOperator op, EqualityOperator equality, char first)
		: base(op) => (First, Equality) = (first, equality);

	public override string ToString() => $".{String(Operator)}(FirstChar {String(Equality)} '{First}')";
}

public sealed class LastNode : Node
{
	public char Last { get; internal set; }

	public EqualityOperator Equality { get; internal set; }

	public string Description => $"=> {String(Equality == EqualityOperator.Equal)}{Last}";

	public string ShortDescription => $"{String(Equality == EqualityOperator.Equal)}{Last}";

	public LastNode(NodeOperator op, EqualityOperator equality, char last)
		: base(op) => (Last, Equality) = (last, equality);

	public override string ToString() => $".{String(Operator)}(LastChar {String(Equality)} '{Last}')";
}

public sealed class LengthNode : Node
{
	public int Length { get; internal set; }

	public ComparisonOperator Comparison { get; internal set; }

	public string Description => $"{String(Comparison)} {Length}";

	public LengthNode(NodeOperator op, int length, ComparisonOperator comparison)
		: base(op) => (Length, Comparison) = (length, comparison);

	public override string ToString() => $".{String(Operator)}(Length {String(Comparison)} {Length})";
}

public sealed class TypeNode : Node
{
	public WordType Type { get; internal set; }

	public bool Contains { get; internal set; }

	public string Description => $"{String(Contains)}{Space(Contains)}${Type.TypeToString()}";

	public string ShortDescription => $"{String(Contains)}{Space(Contains)}${Type.TypeToChar()}";

	public TypeNode(NodeOperator op, WordType type, bool contains)
		: base(op) => (Type, Contains) = (type, contains);

	public override string ToString()
		=> $".{String(Operator)}({String(Contains)}Contains(WordType.{Type}))";
}

public sealed class WildcardNode : Node
{
	public string Pattern { get; internal set; }

	public bool Matches { get; internal set; }

	public string Description
	{
		get
		{
			var matches = String(Matches);
			var space = Space(Matches);
			var pattern = Pattern.Length < 12 ? Pattern : $"{Pattern.AsSpan()[..12]}...";
			return $"{matches}{space}'{pattern}'";
		}
	}

	private string RegexPattern => WildcardSyntax.Parse(Pattern);

	public WildcardNode(NodeOperator op, string pattern, bool matches)
		: base(op) => (Pattern, Matches) = (pattern, matches);

	public override string ToString()
		=> $".{String(Operator)}({String(Matches)}{nameof(Extensions.TreeSearchHelper.__Regex_IsMatch__)}(\"{RegexPattern}\"))";
}

public sealed class RegexNode : Node
{
	public string Pattern { get; internal set; }

	public bool Matches { get; internal set; }

	public string Description
	{
		get
		{
			var matches = String(Matches);
			var space = Space(Matches);
			var pattern = Pattern.Length < 12 ? Pattern : $"{Pattern.AsSpan()[..12]}...";
			return $"{matches}{space}/{pattern}/";
		}
	}

	public RegexNode(NodeOperator op, string pattern, bool matches)
		: base(op) => (Pattern, Matches) = (pattern, matches);

	public override string ToString()
		=> $".{String(Operator)}({String(Matches)}{nameof(Extensions.TreeSearchHelper.__Regex_IsMatch__)}(\"{Pattern}\"))";
}

public sealed class SpecializedNode : Node
{
	public SpecializedCondition Condition { get; internal set; }

	public bool Fulfills { get; internal set; }

	public string Description 
	{
		get
		{
			var condition = Condition switch
			{
				SpecializedCondition.SingleTyped => "単タイプ",
				SpecializedCondition.DoubleTyped => "複合タイプ",
				SpecializedCondition.Killable => "8X",
				SpecializedCondition.Semikillable => "6X",
				SpecializedCondition.Danger4 => "D4",
				SpecializedCondition.Fourxable => "4X",
				_ => "不明な条件"
			};
			return $"{String(Fulfills)}{Space(Fulfills)}{condition}"; 
		}
	}

	public SpecializedNode(NodeOperator op, SpecializedCondition condition, bool fulfills)
		: base(op) => (Condition, Fulfills) = (condition, fulfills);

	public override string ToString() => $".{String(Operator)}({String(Fulfills)}{String(Condition)})";
}

public sealed class GroupNode(NodeOperator op, IReadOnlyList<Node> nodes) : Node(op)
{
	public IReadOnlyList<Node> Nodes { get; internal set; } = nodes;

	public override string ToString() => $".{String(Operator)}({nameof(Extensions.BooleanEvaluator)}(){Nodes.Select(n => n.ToString()).StringJoin()}.{nameof(Extensions.BooleanEvaluator.Evaluate)}())";
}
