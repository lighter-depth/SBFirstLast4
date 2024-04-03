namespace SBFirstLast4.Dynamic;

public static class ModuleManager
{
	public static List<Module> Modules { get; private set; } = [];

	public static Module UserDefined { get; private set; } = new() { Name = "USER_DEFINED", Symbols = { "USER_DEFINED" } };

	public static string[] ModuleNames => Modules.Select(x => x.Name).Except(ExcludedModules).ToArray();

	public static Macro[] Macros => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Macros).Concat(UserDefined.Macros).ToArray();

	public static Macro[] Ephemerals => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Ephemerals).Concat(UserDefined.Ephemerals).ToArray();

	public static Procedure[] Procedures => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Procedures).Concat(UserDefined.Procedures).ToArray();

	public static string[] Symbols => Modules.Where(m => !ExcludedModules.Contains(m.Name)).SelectMany(m => m.Symbols).Concat(UserDefined.Symbols).Distinct().ToArray();

	public static string[] ContentNames => Macros.Select(x => x.Name).Concat(Ephemerals.Select(x => x.Name)).Concat(Symbols).ToArray();

	public static List<string> ExcludedModules { get; private set; } = [];

	public static Module[] RuntimeModules => Modules.Where(m => m.IsRuntime).ToArray();


	private static readonly List<Module> WaitingQueue = [];

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
		status = $"Added module {module.Name} to waiting queue. requires: [{module.Requires.StringJoin(", ")}], requiring: [{module.Requires.Except(ModuleNames).StringJoin(", ")}]";
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
