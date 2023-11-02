namespace SBFirstLast4.Dynamic;

public abstract class Macro
{
	public abstract required string Name { get; init; }

	public required string ModuleName { get; init; }
}

public enum MacroType { None, ObjectLike, FunctionLike }

public class ObjectLikeMacro : Macro
{
	public override required string Name { get; init; }

	public required string Body { get; init; }
}

public class FunctionLikeMacro : Macro
{
	public override required string Name { get; init; }

	public required List<string> Parameters { get; init; }

	public required string Body { get; init; }
}
