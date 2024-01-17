using System.Text.RegularExpressions;

namespace SBFirstLast4.Dynamic;

public abstract class Macro
{
	public abstract required string Name { get; init; }

	public required string ModuleName { get; init; }

	public static string ExpandEphemeral(string input)
	{
		foreach (var macro in ModuleManager.Ephemerals.Reverse())
		{
			if (macro is FunctionLikeMacro functionLikeMacro)
			{
				input = Regex.Replace(input, $@"{functionLikeMacro.Name}\((?<parameters>[^)]+)\)", m =>
				{
					if (Is.InsideStringLiteral(m.Index, m.Length, input))
						return m.Value;

					var args = m.Groups["parameters"].Value.Split(',').Select(arg => arg.Trim()).ToList();
					var body = functionLikeMacro.Body;
					for (var i = 0; i < functionLikeMacro.Parameters.Count; i++)
						body = body.Replace(functionLikeMacro.Parameters[i], args[i]);
					return body;
				});
				continue;
			}
			if (macro is ObjectLikeMacro objectLikeMacro)
				input = input.ReplaceFreeString(objectLikeMacro.Name, objectLikeMacro.Body);
		}
		return input;
	}

	public static string Expand(string input)
	{
		foreach (var macro in ModuleManager.Macros.Reverse())
		{
			if (macro is FunctionLikeMacro functionLikeMacro)
			{
				input = Regex.Replace(input, $@"{functionLikeMacro.Name}\((?<parameters>[^)]+)\)", m =>
				{
					if (Is.InsideStringLiteral(m.Index, m.Length, input))
						return m.Value;

					var args = m.Groups["parameters"].Value.Split(',').Select(arg => arg.Trim()).ToList();
					var body = functionLikeMacro.Body;
					for (var i = 0; i < functionLikeMacro.Parameters.Count; i++)
						body = body.Replace(functionLikeMacro.Parameters[i], args[i]);
					return body;
				});
				continue;
			}
			if (macro is ObjectLikeMacro objectLikeMacro)
				input = input.ReplaceFreeString(objectLikeMacro.Name, objectLikeMacro.Body);
		}
		return input;
	}
}

public enum MacroType { None, ObjectLike, FunctionLike }

public class ObjectLikeMacro : Macro
{
	public override required string Name { get; init; }

	public required string Body { get; set; }

}

public class FunctionLikeMacro : Macro
{
	public override required string Name { get; init; }

	public required List<string> Parameters { get; init; }

	public required string Body { get; set; }
}
