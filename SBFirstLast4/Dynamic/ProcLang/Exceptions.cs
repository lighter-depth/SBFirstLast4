namespace SBFirstLast4.Dynamic;

public class Return : Exception
{
	public object? Value { get; }

	public Return(object? value) => Value = value;
}

public class Break : Exception { }

public class Continue : Exception { }

public class Redo : Exception { }
