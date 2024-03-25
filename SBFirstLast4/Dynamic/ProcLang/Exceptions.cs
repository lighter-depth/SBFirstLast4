namespace SBFirstLast4.Dynamic;

public class Return(object? value) : Exception
{
	public object? Value { get; } = value;
}

public class Raise(string name) : Exception
{
	public string Name { get; } = name;
}

public class Break : Exception;

public class Continue : Exception;

public class Redo : Exception;

public class Retry : Exception;

public class Halt(string? message = "Procedure terminated.") : Exception(message);