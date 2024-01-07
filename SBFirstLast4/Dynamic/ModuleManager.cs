using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Dynamic;

public static class ModuleManager
{
	public static List<Module> Modules { get; private set; } = new();

	public static Module UserDefined { get; private set; } = new() { Name = "USER_DEFINED", Symbols = { "USER_DEFINED" } };

	public static string[] ModuleNames => Modules.Select(x => x.Name).Except(ExcludedModules).ToArray();

	public static Macro[] Macros => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Macros).Concat(UserDefined.Macros).ToArray();

	public static Macro[] Ephemerals => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Ephemerals).Concat(UserDefined.Ephemerals).ToArray();

	public static string[] Symbols => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Symbols).Concat(UserDefined.Symbols).Distinct().ToArray();

	public static string[] ContentNames => Macros.Select(x => x.Name).Concat(Ephemerals.Select(x => x.Name)).Concat(Symbols).ToArray();

	public static List<string> ExcludedModules { get; private set; } = new();

	public static Module[] RuntimeModules => Modules.Where(m => m.IsRuntime).ToArray();


	private static readonly List<Module> WaitingQueue = new();

	public static Module? GetModule(string? moduleName) => Modules.Where(m => m.Name == moduleName).FirstOrDefault();

	public static bool Import(Module? module, out string status)
	{
		if (module is null)
		{
			status = "Invalid module format.";
			return false;
		}

		if (module.Requires.Length == 0 || module.Requires.All(r => Modules.Select(m => m.Name).Contains(r)))
		{
			Modules.ReplaceOrAdd(module);
			EvaluateWaitingQueue();
			status = $"Successfully added module {module.Name}.";
			return true;
		}

		WaitingQueue.ReplaceOrAdd(module);
		status = $"Added module {module.Name} to waiting queue. requires: [{module.Requires.Stringify()}], requiring: [{module.Requires.Except(ModuleNames).Stringify()}]";
		return true;
	}

	internal static void AddModule(Module? module)
	{
		if (module is null) return;

		if (module.Requires.Length == 0 || module.Requires.All(r => Modules.Select(m => m.Name).Contains(r)))
		{
			Modules.Add(module);
			EvaluateWaitingQueue();
			return;
		}

		WaitingQueue.Add(module);
	}

	private static void EvaluateWaitingQueue()
	{
		var addedModules = new List<string>();
		foreach (var module in WaitingQueue)
		{
			if (!module.Requires.All(r => Modules.Select(m => m.Name).Contains(r)))
				continue;

			Modules.Add(module);
			addedModules.Add(module.Name);
		}
		WaitingQueue.RemoveAll(m => addedModules.Contains(m.Name));
	}

	public static bool Exclude(string moduleName)
	{
		if (moduleName is "$ALL")
		{
			ExcludedModules.AddRange(ModuleNames);
			return true;
		}

		if (!ModuleNames.Contains(moduleName))
			return false;

		ExcludedModules.Add(moduleName);

		foreach (var module in Modules)
			if (module.Requires.Contains(moduleName))
				Exclude(module.Name);

		return true;
	}

	public static bool Include(string moduleName)
	{
		if (moduleName is "$ALL")
		{
			ExcludedModules.Clear();
			return true;
		}

		if (!ExcludedModules.Remove(moduleName))
			return false;

		var namesToInclude = new List<string>();

		foreach (var excludedName in ExcludedModules)
			if (Modules.FirstOrDefault(m => m.Name == excludedName)?.Requires.All(r => ModuleNames.Contains(r)) ?? false)
				namesToInclude.Add(excludedName);

		foreach (var name in namesToInclude)
			Include(name);

		return true;
	}

	public static bool Delete(string moduleName, out string status)
	{
		var moduleNames = RuntimeModules.Concat(WaitingQueue.Where(m => m.IsRuntime)).Select(m => m.Name);

		if (moduleName == "$ALL")
		{
			foreach (var m in moduleNames)
				Delete(m, out _);

			status = "Successfully deleted all runtime modules.";
			return true;
		}
		if (!moduleNames.Contains(moduleName))
		{
			status = $"Specified runtime module {moduleName} does not exist.";
			return false;
		}

		var queueIndex = WaitingQueue.FindIndex(m => m.Name == moduleName);
		if (queueIndex != -1)
		{
			WaitingQueue.RemoveAt(queueIndex);
			status = $"Successfully deleted module {moduleName} from waiting queue.";
			return true;
		}

		var modulesToDelete = new List<string> { moduleName };

		foreach (var module in RuntimeModules)
			if (module.Requires.Contains(moduleName))
				modulesToDelete.Add(module.Name);

		Modules = Modules.Where(m => m.Name != moduleName).ToList();

		foreach (var m in modulesToDelete)
			Delete(m, out _);

		status = $"Successfully deleted module {moduleName} and all dependent modules.";
		return true;
	}

	public static bool DeleteByRuntimeName(string runtimeName, out string status)
	{
		var moduleName = RuntimeModules
			.Concat(WaitingQueue)
			.Where(m => m.RuntimeName == runtimeName)
			.FirstOrDefault()
			?.Name;

		if (moduleName is null)
		{
			status = $"Specified runtime module {moduleName} does not exist.";
			return false;
		}

		var queueIndex = WaitingQueue.FindIndex(m => m.Name == moduleName);
		if (queueIndex != -1)
		{
			WaitingQueue.RemoveAt(queueIndex);
			status = $"Successfully deleted module {moduleName} from waiting queue.";
			return true;
		}

		return Delete(moduleName, out status);
	}
}

public partial class Module : IEquatable<Module>
{
	public required string Name { get; init; }

	public string[] Requires { get; init; } = Array.Empty<string>();

	public List<string> Symbols { get; init; } = new();

	public List<Macro> Macros { get; init; } = new();

	public List<Macro> Ephemerals { get; init; } = new();

	public bool IsRuntime { get; init; }

	public string? RuntimeName { get; init; }

    public override bool Equals(object? obj) => Equals(obj as Module);

	public override int GetHashCode() => Name.GetHashCode();

    public bool Equals(Module? other) => Name == other?.Name;

    public static Module? Compile(string moduleContent, bool isRuntime = false, string? runtimeName = null)
	{
		var match = ModuleRegex().Match(moduleContent);
		if (!match.Success) return null;

		var name = match.Groups["name"].Value;
		var requires = match.Groups["requires"].Value;
		var content = match.Groups["contents"].Value;
		var contents = content
			.Split(Environment.NewLine)
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Select(s => s.Trim())
			.ToArray();

		var reader = new ContentReader(name).ReadContents(contents);

		return new()
		{
			Name = name,
			Requires = string.IsNullOrWhiteSpace(requires)
						? Array.Empty<string>()
						: requires.Trim().Split(",").Select(s => s.Trim()).ToArray(),
			Symbols = reader.Symbols,
			Macros = reader.Macros,
			Ephemerals = reader.Ephemerals,
			IsRuntime = isRuntime,
			RuntimeName = runtimeName
		};
	}



	[GeneratedRegex(@"^[\s\r\n]*module\s+(?<name>\w+)[\s\r\n]*;[\s\r\n]*(?:requires:(?<requires>[\s\S]*?);[\s\r\n]*)?(?<contents>[\s\S]*)$")]
	internal static partial Regex ModuleRegex();

	[GeneratedRegex(@"define\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex DefineObjectLikeMacroRegex();

	[GeneratedRegex(@"define\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex DefineFunctionLikeMacroRegex();

	[GeneratedRegex(@"ephemeral\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex EphemeralObjectLikeMacroRegex();

	[GeneratedRegex(@"ephemeral\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex EphemeralFunctionLikeMacroRegex();

	[GeneratedRegex(@"transient\s+(?<key>[^\s]+)\s+(?<value>.*)")]
	internal static partial Regex TransientObjectLikeMacroRegex();

	[GeneratedRegex(@"transient\s+(?<name>\w+)\((?<parameters>[^)]+)\)\s+(?<body>.+)")]
	internal static partial Regex TransientFunctionLikeMacroRegex();
}

file class ContentReader
{
	private readonly string ModuleName;
	private readonly Stack<bool> IsDisabled;
	private readonly Stack<bool> IfDefStack = new();

	internal List<string> Symbols { get; private set; } = new();
	internal List<Macro> Macros { get; private set; } = new();
	internal List<Macro> Ephemerals { get; private set; } = new();

	private readonly List<Macro> Transients = new();
	private readonly List<string> OmitSymbols = new();

	public ContentReader(string moduleName)
	{
		ModuleName = moduleName;
		IsDisabled = new();
		IsDisabled.Push(false);
	}


	internal ContentReader ReadContents(string[] contents)
	{
		foreach (var content in contents)
			ProcessDirective(content);

		foreach (var macro in Macros)
			switch (macro)
			{
				case ObjectLikeMacro objectLike:
					{
						var body = objectLike.Body;

						foreach (var transient in Transients)
							body = ExpandTransient(body, transient);

						objectLike.Body = body;

						break;
					}
				case FunctionLikeMacro functionLike:
					{
						var body = functionLike.Body;

						foreach (var transient in Transients)
							body = ExpandTransient(body, transient);

						functionLike.Body = body;

						break;
					}
				default:
					break;
			}
		return this;
	}



	private void ProcessDirective(string directiveStr)
	{
		var directive = directiveStr.Split();
		switch (directive.At(0))
		{
			case "#define":
				{
					if (IsDisabled.Peek() || directive.Length < 2)
						break;

					if (directive.Length == 2)
					{
						Symbols.Add(directive[1]);
						break;
					}

					var match = Module.DefineFunctionLikeMacroRegex().Match(directiveStr);
					if (match.Success)
					{
						var functionLikeMacro = new FunctionLikeMacro
						{
							Name = match.Groups["name"].Value,
							Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
							Body = match.Groups["body"].Value,
							ModuleName = ModuleName
						};
						Macros.Add(functionLikeMacro);
						break;
					}

					var groups = Module.DefineObjectLikeMacroRegex().Match(directiveStr).Groups;
					var objectLikeMacro = new ObjectLikeMacro
					{
						Name = groups["key"].Value,
						Body = groups["value"].Value,
						ModuleName = ModuleName
					};
					Macros.Add(objectLikeMacro);
					break;
				}
			case "#undef":
				{
					if (IsDisabled.Peek() || directive.Length < 2)
						break;

					var symbol = directive[1];
					OmitSymbols.Add(symbol);
					Symbols.RemoveAll(s => s == symbol);
					Macros.RemoveAll(m => m.Name == symbol);
					break;
				}
			case "#ephemeral":
				{
					if (IsDisabled.Peek() || directive.Length < 3)
						break;

					var match = Module.EphemeralFunctionLikeMacroRegex().Match(directiveStr);
					if (match.Success)
					{
						var functionLikeMacro = new FunctionLikeMacro
						{
							Name = match.Groups["name"].Value,
							Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
							Body = match.Groups["body"].Value,
							ModuleName = ModuleName
						};
						Ephemerals.Add(functionLikeMacro);
						break;
					}

					var groups = Module.EphemeralObjectLikeMacroRegex().Match(directiveStr).Groups;
					var objectLikeMacro = new ObjectLikeMacro
					{
						Name = groups["key"].Value,
						Body = groups["value"].Value,
						ModuleName = ModuleName
					};
					Ephemerals.Add(objectLikeMacro);
					break;
				}
			case "#evaporate":
				{
					if (IsDisabled.Peek() || directive.Length < 2)
						break;

					var symbol = directive[1];
					OmitSymbols.Add(symbol);
					Symbols.RemoveAll(s => s == symbol);
					Ephemerals.RemoveAll(m => m.Name == symbol);
					break;
				}
			case "#transient":
				{
					if (IsDisabled.Peek() || directive.Length < 2)
						break;

					if (directive.Length == 2)
					{
						Symbols.Add(directive[1]);
						break;
					}

					var match = Module.TransientFunctionLikeMacroRegex().Match(directiveStr);
					if (match.Success)
					{
						var functionLikeTransient = new FunctionLikeMacro
						{
							Name = match.Groups["name"].Value,
							Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
							Body = match.Groups["body"].Value,
							ModuleName = "__TRANSIENT__"
						};
						Transients.Add(functionLikeTransient);
						break;
					}

					var groups = Module.TransientObjectLikeMacroRegex().Match(directiveStr).Groups;
					var objectLikeMacro = new ObjectLikeMacro
					{
						Name = groups["key"].Value,
						Body = groups["value"].Value,
						ModuleName = "__TRANSIENT__"
					};
					Transients.Add(objectLikeMacro);
					break;
				}
			case "#ifdef":
				{
					var isDefined = ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					IfDefStack.Push(isDefined);
					IsDisabled.Push(IsDisabled.Peek() || !isDefined);
					break;
				}

			case "#ifndef":
				{
					var isNotDefined = !ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					IfDefStack.Push(isNotDefined);
					IsDisabled.Push(IsDisabled.Peek() || !isNotDefined);
					break;
				}

			case "#elifdef":
				{
					if (IfDefStack.Count <= 0)
						break;

					var prevResult = IfDefStack.Pop();
					var isDefined = ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					IfDefStack.Push(isDefined);
					IsDisabled.Pop();
					IsDisabled.Push(IsDisabled.Peek() || prevResult || !isDefined);
					break;
				}

			case "#elifndef":
				{
					if (IfDefStack.Count <= 0)
						break;

					var prevResult = IfDefStack.Pop();
					var isNotDefined = !ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					IfDefStack.Push(isNotDefined);
					IsDisabled.Pop();
					IsDisabled.Push(IsDisabled.Peek() || prevResult || !isNotDefined);
					break;
				}
			case "#else":
				{
					if (IfDefStack.Count <= 0)
						break;

					var prevResult = IfDefStack.Pop();
					IfDefStack.Push(!prevResult);
					IsDisabled.Pop();
					IsDisabled.Push(IsDisabled.Peek() || prevResult);
					break;
				}
			case "#endif":
				{
					if (IfDefStack.Count <= 0)
						break;

					IfDefStack.Pop();
					IsDisabled.Pop();
					break;
				}

			default:
				break;
		}
	}


	private static string ExpandTransient(string input, Macro transient)
	{
		if (transient is FunctionLikeMacro functionLikeTransient)
		{
			input = Regex.Replace(input, $@"{functionLikeTransient.Name}\((?<parameters>[^)]+)\)", m =>
			{
				var args = m.Groups["parameters"].Value.Split(',').Select(arg => arg.Trim()).ToList();
				var body = functionLikeTransient.Body;
				for (var i = 0; i < functionLikeTransient.Parameters.Count; i++)
					body = body.Replace(functionLikeTransient.Parameters[i], args[i]);
				return body;
			});
		}
		else if (transient is ObjectLikeMacro objectLikeTransient)
			input = input.Replace(objectLikeTransient.Name, objectLikeTransient.Body);

		return input;

	}
}
