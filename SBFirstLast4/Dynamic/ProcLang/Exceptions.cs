namespace SBFirstLast4.Dynamic;

public class Return : Exception
{
	public object? Value { get; }

	public Return(object? value) => Value = value;
}

public class Raise : Exception
{
	public string Name { get; }

	public Raise(string name) => Name = name;
}

public class Break : Exception { }

public class Continue : Exception { }

public class Redo : Exception { }

public class Retry : Exception { }

public class Halt : Exception 
{
	public Halt(string? message = "Procedure terminated.") : base(message) { }
}