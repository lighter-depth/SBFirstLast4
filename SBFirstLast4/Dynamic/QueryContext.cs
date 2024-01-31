namespace SBFirstLast4.Dynamic;

public enum QueryContext
{
	Script,
	Module,
	AnonymousProcedure,
	NamedProcedure,
	RunningProcedure,
	Init,
	Static,
	Namespace
}

public static class QueryContexts 
{
	public static readonly string[] ScriptContextSpecifiers =
	{
		".default", ".end", ".proc", ".script", ".module"
	};

	public static readonly string[] ModuleContextSpecifiers =
	{
		".default", ".end", ".proc", ".module", ".init", ".static"
	};
}
