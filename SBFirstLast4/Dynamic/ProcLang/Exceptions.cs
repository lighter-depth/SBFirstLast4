namespace SBFirstLast4.Dynamic;

public sealed class Return(object? value) : Exception
{
	public object? Value { get; } = value;
}

public sealed class Raise(string name) : Exception
{
	public string Name { get; } = name;
}

public sealed class Break : Exception;

public sealed class Continue : Exception;

public sealed class Redo : Exception;

public sealed class Retry : Exception;

public sealed class Halt(string? message = "Procedure terminated.") : Exception(message);