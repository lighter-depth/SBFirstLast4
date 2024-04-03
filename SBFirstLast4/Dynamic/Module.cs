using System.Text.RegularExpressions;

namespace SBFirstLast4.Dynamic;

public sealed class Module : IEquatable<Module>
{
	public required string Name { get; init; }

	public string[] Requires { get; init; } = [];

	public List<string> Symbols { get; init; } = [];

	public List<Macro> Macros { get; init; } = [];

	public List<Macro> Ephemerals { get; init; } = [];

	public List<Procedure> Procedures { get; init; } = [];

	public List<string> InitStatements { get; init; } = [];

	public List<string> StaticStatements { get; init; } = [];

	public string ModuleContent { get; init; } = string.Empty;

	public bool IsRuntime { get; init; }

	public string? RuntimeName { get; init; }

	public override bool Equals(object? obj) => Equals(obj as Module);

	public override int GetHashCode() => Name.GetHashCode();

	public bool Equals(Module? other) => Name == other?.Name;

	public static Module? Compile(string moduleContent, bool isRuntime = false, string? runtimeName = null)
	{
		var match = ModuleRegex.Module().Match(moduleContent);
		if (!match.Success) return null;

		var name = match.Groups["name"].Value;
		var requires = match.Groups["requires"].Value;
		var content = match.Groups["contents"].Value;
		var contents = content
			.Split(Environment.NewLine)
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Select(s => s.Trim())
			.ToArray();

		var reader = new ModuleReader(name).ReadContents(contents);

		return new()
		{
			Name = name,
			Requires = string.IsNullOrWhiteSpace(requires)
						? []
						: requires.Trim().Split(",").Select(s => s.Trim()).ToArray(),
			Symbols = reader.Symbols,
			Macros = reader.Macros,
			Ephemerals = reader.Ephemerals,
			Procedures = reader.Procedures,
			InitStatements = reader.InitStatements,
			StaticStatements = reader.StaticStatements,
			ModuleContent = moduleContent,
			IsRuntime = isRuntime,
			RuntimeName = runtimeName
		};
	}
}

internal static partial class ModuleRegex
{
	[GeneratedRegex(@"^[\s\r\n]*module\s+(?<name>\w+)[\s\r\n]*;[\s\r\n]*(?:requires:(?<requires>[\s\S]*?);[\s\r\n]*)?(?<contents>[\s\S]*)$")]
	internal static partial Regex Module();

	[GeneratedRegex(@"define\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex DefineObjectLikeMacro();

	[GeneratedRegex(@"define\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex DefineFunctionLikeMacro();

	[GeneratedRegex(@"ephemeral\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex EphemeralObjectLikeMacro();

	[GeneratedRegex(@"ephemeral\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex EphemeralFunctionLikeMacro();

	[GeneratedRegex(@"transient\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex TransientObjectLikeMacro();

	[GeneratedRegex(@"transient\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex TransientFunctionLikeMacro();
}
