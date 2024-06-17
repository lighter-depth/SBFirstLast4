namespace SBFirstLast4.Dynamic;

internal sealed class Scope(Action onExit)
{
	private int _depth;

	private readonly Action _onExit = onExit;

	public void Enter() => _depth++;

	public void Exit() 
	{
		_onExit();
		_depth--;
	}

	public Func<ScopedVariable, bool> Predicate => v => v.Depth == _depth;

	public ScopedVariable Register(string name)
		=> new(name, _depth);
}

internal readonly record struct ScopedVariable(string Name, int Depth);
