using static SBFirstLast4.Expressions.OperatorExtension;

namespace SBFirstLast4.Expressions;

public class Node
{
	public NodeOperator Operator { get; internal set; }

	public Node(NodeOperator op) => Operator = op;
}

public enum NodeType
{
	First, Last, Length, Type, Regex, Group
}


public class FirstNode : Node 
{
	public char First { get; internal set; }

	public EqualityOperator Equality { get; internal set; }

	public FirstNode(NodeOperator op, EqualityOperator equality, char first)
		: base(op) => (First, Equality) = (first, equality);

	public override string ToString() => $"{String(Operator)}(FirstChar {String(Equality)} '{First}')";
}

public class LastNode : Node
{
	public char Last { get; internal set; }

	public EqualityOperator Equality { get; internal set; }

	public LastNode(NodeOperator op, EqualityOperator equality, char last)
		: base(op) => (Last, Equality) = (last, equality);

	public override string ToString() => $"{String(Operator)}(LastChar {String(Equality)} '{Last}')";
}

public class LengthNode : Node
{
	public int Length { get; internal set; }

	public ComparisonOperator Comparison { get; internal set; }

	public LengthNode(NodeOperator op, int length, ComparisonOperator comparison)
		: base(op) => (Length, Comparison) = (length, comparison);

	public override string ToString() => $"{String(Operator)}(Length {String(Comparison)} {Length})";
}

public class TypeNode : Node
{
	public WordType Type { get; internal set; }

	private bool IsSingleTyped { get; init; }

	public TypeNode(NodeOperator op, WordType type)
		: base(op) => Type = type;

	public static TypeNode SingleTypeNode(NodeOperator op, WordType type, bool isSingleTyped)
		=> new(op, type) { IsSingleTyped = isSingleTyped };

	public override string ToString() 
		=> $"{String(Operator)}(Contains(WordType.{Type}) {(IsSingleTyped ? "&& IsSingleType" : string.Empty)})";
}

public class RegexNode : Node 
{
	public string Pattern { get; internal set; }

	public RegexNode(NodeOperator op, string pattern)
		: base(op) => Pattern = pattern;
}

public class GroupNode : Node
{
	public IReadOnlyList<Node> Nodes { get; internal set; }

	public GroupNode(NodeOperator op,  IReadOnlyList<Node> nodes)
		: base(op) => Nodes = nodes;

	public override string ToString() => $"{String(Operator)}({Nodes.Select(n => n.ToString()).StringJoin()})";
}
