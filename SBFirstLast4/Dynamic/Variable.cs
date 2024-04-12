namespace SBFirstLast4.Dynamic;

public sealed class Variable<T>(string name, T value, bool isReadOnly, bool isAssignable)
{
	public readonly string Name = name;

	private T _value = value;

	public T Value 
	{
		get => _value;
		set => _value = !IsReadOnly ? value : throw new ReadOnlyViolationException($"Variable '{Name}' is readonly.");
	}

	public readonly bool IsReadOnly = isReadOnly;

	public readonly bool IsAssignable = isAssignable;
}

public sealed class ReadOnlyViolationException(string? message) : Exception(message);

public sealed class ConstViolationException(string? message) : Exception(message);
