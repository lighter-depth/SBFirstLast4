namespace SBFirstLast4.Expression;

public abstract class Node
{
	public abstract string Type { get; }

	public Pipe Pipe { get; internal set; } = Pipe.None;

	public bool IsIdentity { get; internal set; } = true;

	public static Node Create(string type) => type switch
	{
		NodeType.First => new FirstNode(),
		NodeType.Last => new LastNode(),
		NodeType.Length => new LengthNode(),
		NodeType.Type => new TypeNode(),
		NodeType.Regex => new RegexNode(),
		_ => throw new ArgumentException(nameof(NodeType)),
	};
}

public sealed class FirstNode : Node
{
	public override string Type => NodeType.First;

	public char Value { get; internal set; }

	public bool IsEqual { get; internal set; } = true;
}

public sealed class LastNode : Node
{
	public override string Type => NodeType.Last;

	public char Value { get; internal set; }

	public bool IsEqual { get; internal set; } = true;
}

public sealed class LengthNode : Node
{
	public override string Type => NodeType.Length;

	public string Attribute { get; internal set; } = LengthAttribute.GreaterOrEqualTo;
}

public sealed class TypeNode : Node
{
	public override string Type => NodeType.Type;

	public WordType WordType { get; internal set; }
}

public sealed class RegexNode : Node
{
	public override string Type => NodeType.Regex;

	public string Pattern { get; internal set; } = string.Empty;
}

public static class NodeType
{
	public const string First = "最初の文字",
						Last = "最後の文字",
						Length = "文字数",
						Type = "タイプ",
						Regex = "正規表現";

	public static readonly string[] Types = { First, Last, Length, Type, Regex };
}

public static class LengthAttribute
{
	public const string GreaterOrEqualTo = "以上",
						LesserOrEqualTo = "以下",
						Greater = "より大きい",
						Lesser = "より小さい",
						EqualTo = "と等しい",
						NotEqualTo = "でない";

	public static readonly string[] Attributes = { GreaterOrEqualTo, LesserOrEqualTo, Greater, Lesser, EqualTo, NotEqualTo };
}

public enum Pipe { None, And, Or, Xor, Nand, Nor, Xnor, Imply, Nimply }

public static class BooleanOperators
{
	public static bool And(bool left, bool right) => left && right;

	public static bool Or(bool left, bool right) => left || right;

	public static bool Xor(bool left, bool right) => left ^ right;

	public static bool Nand(bool left, bool right) => !And(left, right);

	public static bool Nor(bool left, bool right) => !Or(left, right);

	public static bool Xnor(bool left, bool right) => !Xor(left, right);

	public static bool Imply(bool left, bool right) => Or(!left, right);

	public static bool Nimply(bool left, bool right) => !Imply(left, right);
}


public record PipeElement(Func<Word, bool> Predicate, Pipe Pipe)
{

	public bool this[Word word] => Predicate(word);
}
