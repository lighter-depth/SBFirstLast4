namespace SBFirstLast4.Expressions;

public sealed class ShowMenuUI
{
	public bool Node { get; set; }

	public bool First { get; set; }

	public bool Last { get; set; }

	public bool Length { get; set; }

	public bool Type { get; set; }

	public bool Wildcard { get; set; }

	public bool Regex { get; set; }

	public bool Specialized { get; set; }

	public bool Group { get; set; }

	public bool IsDefault
		=> (Node, First, Last, Length, Type, Wildcard, Regex, Specialized, Group) == _tuple;

	public ShowMenuUI()
		=> AlterDefault();

	private static readonly (bool, bool, bool, bool, bool, bool, bool, bool, bool) _tuple = default;

	public void Alter(bool to, string target)
	{
		switch (target)
		{
			case nameof(Node): Node = to; return;
			case nameof(First): First = to; return;
			case nameof(Last): Last = to; return;
			case nameof(Length): Length = to; return;
			case nameof(Type): Type = to; return;
			case nameof(Wildcard): Wildcard = to; return;
			case nameof(Regex): Regex = to; return;
			case nameof(Specialized): Specialized = to; return;
			case nameof(Group): Group = to; return;
		}
	}

	public void AlterDefault()
		=> (Node, First, Last, Length, Type, Wildcard, Regex, Specialized, Group) = _tuple;
}

public sealed class ActionWrapper
{
	private Action _action = () => { };

	public Action Add(Action action) => _action += action;

	public Action Remove(Action action) => _action = _action - action ?? (() => { });

	public void Invoke() => _action();
}