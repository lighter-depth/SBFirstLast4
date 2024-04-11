namespace SBFirstLast4.Expressions;

public class NodeBuilder
{
	public NodeOperator Operator { private protected get; set; }

	public virtual Node Build() => new(Operator);
}

public sealed class FirstNodeBuilder : NodeBuilder
{
	public EqualityOperator Equality { private get; set; }

	public char First { private get; set; }

	public override FirstNode Build() => new(Operator, Equality, First);
}

public sealed class LastNodeBuilder : NodeBuilder 
{
	public EqualityOperator Equality { private get; set; }

	public char Last { private get; set; }

	public override LastNode Build() => new(Operator, Equality, Last);
}

public sealed class LengthNodeBuilder : NodeBuilder
{
	public int Length { private get; set; }

	public ComparisonOperator Comparison { private get; set; }

	public override LengthNode Build() => new(Operator, Length, Comparison);
}

public sealed class TypeNodeBuilder : NodeBuilder 
{
	public WordType Type { private get; set; }

	public bool Contains { private get; set; }

	public override TypeNode Build() => new(Operator, Type, Contains);
}

public sealed class WildcardNodeBuilder : NodeBuilder
{
	public string Pattern { private get; set; } = string.Empty;

	public bool Matches { private get; set; }

	public override WildcardNode Build() => new(Operator, Pattern, Matches);
}

public sealed class RegexNodeBuilder : NodeBuilder
{
	public string Pattern { private get; set; } = string.Empty;

	public bool Matches { private get; set; }

	public override RegexNode Build() => new(Operator, Pattern, Matches);
}

public sealed class SpecializedNodeBuilder : NodeBuilder
{
	public SpecializedCondition Condition { private get; set; }

	public bool Fulfills { private get; set; }

	public override SpecializedNode Build() => new(Operator, Condition, Fulfills);
}

public sealed class GroupNodeBuilder : NodeBuilder 
{
	public List<Node> Nodes { private get; set; } = [];

	public override GroupNode Build() => new(Operator, Nodes);
}