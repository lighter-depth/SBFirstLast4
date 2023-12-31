﻿using Microsoft.VisualBasic;
using SBFirstLast4.Pages;
using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4.Dynamic;

public static partial class Preprocessor
{
	public static bool IsInitialized { get; private set; } = false;

	private static readonly string[] ValidDirectives =
	{
		"define", "undef", "show", "pragma", "include", "exclude", "ifdef", "ifndef", "delete", "ephemeral", "evaporate"
	};

	private static readonly string[] ModulesToLoad =
	{
		"Standard", "Lists", "Killers", "Tools", "Operators", "MinMax", "Filters"
	};

	public static async Task Initialize(HttpClient client)
	{
		try
		{
			foreach (var module in ModulesToLoad)
				await LoadModule(module, client);

			IsInitialized = true;
		}
		catch (HttpRequestException) { }
	}

	private static async Task LoadModule(string moduleName, HttpClient client)
	{
		var module = await client.GetStringAsync($"https://raw.githubusercontent.com/lighter-depth/SBFirstLast4/sbmdl/modules/{moduleName}.sbmdl");
		ModuleManager.AddModule(Module.Compile(module));
	}

	public static bool TryProcessEphemerals(string input, [NotNullWhen(true)] out string[]? status, [NotNullWhen(false)] out string? errorMsg, string moduleName = "USER_DEFINED")
	{
		(status, errorMsg) = (null, null);
		input = input.Trim();
		if (input.Length < 2)
		{
			errorMsg = "The directive was empty.";
			return false;
		}
		input = input[(input.IndexOf('#') + 1)..];

		var contents = input.Split();
		var symbol = contents.At(0);

		if (symbol is "evaporate")
		{
			if (contents.Length != 2)
			{
				errorMsg = "Invalid syntax: #evaporate syntax must have one argument.";
				return false;
			}
			if (contents[1] == "$ALL")
			{
				ModuleManager.UserDefined.Ephemerals.Clear();
				status = new[] { "Cleared up the USER_DEFINED Ephemeral Dictionary." };
				return true;
			}
			var count = ModuleManager.UserDefined.Ephemerals.RemoveAll(m => m.Name == contents[1]);
			if (count < 1)
			{
				errorMsg = $"Specified ephemeral '{contents[1]}' does not exist in the USER_DEFINED module.";
				return false;
			}
			status = new[] { $"Successfully removed ephemeral '{contents[1]}' from the dictionary." };
			return true;
		}

		if (symbol is "ephemeral")
		{
			var match = Module.EphemeralFunctionLikeMacroRegex().Match(input);
			if (match.Success)
			{
				var functionLikeMacro = new FunctionLikeMacro
				{
					Name = match.Groups["name"].Value,
					Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
					Body = match.Groups["body"].Value,
					ModuleName = moduleName
				};
				ModuleManager.UserDefined.Ephemerals.Add(functionLikeMacro);
				status = new[] { $"Successfully added ephemeral '{match.Groups["name"]}' to the dictionary." };
				return true;
			}

			if (contents.Length < 3)
			{
				errorMsg = "Invalid syntax: #ephemeral syntax must have more than two arguments.";
				return false;
			}

			var groups = Module.EphemeralObjectLikeMacroRegex().Match(input).Groups;
			var objectLikeMacro = new ObjectLikeMacro
			{
				Name = groups["key"].Value,
				Body = groups["value"].Value,
				ModuleName = moduleName
			};
			ModuleManager.UserDefined.Ephemerals.Add(objectLikeMacro);

			status = new[] { $"Successfully added ephemeral '{contents[1]}' to the dictionary." };
			return true;
		}
		errorMsg = $"Invalid directive: {input}";
		return false;
	}


	public static bool TryPreprocess(string input, [NotNullWhen(true)] out string[]? status, [NotNullWhen(false)] out string? errorMsg, string moduleName = "USER_DEFINED")
	{
		(status, errorMsg) = (null, null);
		input = input.Trim();
		if (input.Length < 2)
		{
			errorMsg = "The directive was empty.";
			return false;
		}
		input = input[(input.IndexOf('#') + 1)..];

		var contents = input.Split();
		var symbol = contents.At(0);

		if (symbol is "elifdef" or "elifndef" or "else" or "endif" or "transient")
		{
			errorMsg = "Invalid directive in this context.";
			return false;
		}

		if (!ValidDirectives.Contains(symbol))
		{
			errorMsg = "Couldn't recognize the specified directive type.";
			return false;
		}

		if (symbol is "show")
		{
			var selector = contents.At(1) == default ? "USER_DEFINED" : contents.At(1);

			if (selector is "USER_DEFINED")
			{
				status = ModuleManager.UserDefined.Macros
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL"))
					.Concat(ModuleManager.UserDefined.Ephemerals
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"[[Ephemeral]] Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"[[Ephemeral]] Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL")))
					.Concat(ModuleManager.UserDefined.Symbols.Select(x => $"Symbol: {x}"))
					.ToArray();
				return true;
			}

			if (selector is "$MODULE" or "$MDL")
			{
				status = ModuleManager.ModuleNames.Append("USER_DEFINED").ToArray();
				return true;
			}

			if (selector is "$EXCLUDED" or "$EXC")
			{
				status = ModuleManager.ExcludedModules.ToArray();
				return true;
			}

			if (selector is "$IMPORTED" or "$IMP")
			{
				status = ModuleManager.RuntimeModules.Select(m => m.Name).ToArray();
				return true;
			}

			if (selector is "$ALL")
			{
				status = ModuleManager.Macros
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL"))
					.Concat(ModuleManager.Ephemerals
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"[[Ephemeral]] Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"[[Ephemeral]] Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL")))
					.Concat(ModuleManager.Symbols.Select(x => $"Symbol: {x}"))
					.ToArray();
				return true;
			}

			var module = ModuleManager.GetModule(selector);

			if (module is null)
			{
				errorMsg = $"Specified module {selector} does not exist in module manager.";
				return false;
			}

			status = module.Macros
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL"))
					.Concat(module.Ephemerals
					.Select(x => $"Module: {x.ModuleName}, "
					+ (x is ObjectLikeMacro o
					? $"[[Ephemeral]] Key: {o.Name}, Value: {o.Body}"
					: x is FunctionLikeMacro f
					? $"[[Ephemeral]] Sign: {f.Name}({f.Parameters.StringJoin()}), Body: {f.Body}"
					: "NULL")))
					.Concat(module.Symbols.Select(x => $"Symbol: {x}"))
					.ToArray();
			return true;
		}

		if (symbol is "pragma")
		{
			var pragma = contents.At(1);
			if (pragma is not ("auto" or "reflect" or "monitor" or "easyarray"))
			{
				errorMsg = "Invalid syntax: No applicable pragma found.";
				return false;
			}
			if (pragma is "auto")
			{
				if (contents.Length != 3)
				{
					errorMsg = "Invalid syntax: #pragma auto syntax must have one argument.";
					return false;
				}
				if (contents[2] == "enable")
				{
					Interpreter.IsAuto = true;
					status = new[] { "Auto processing enabled." };
					return true;
				}
				if (contents[2] == "disable")
				{
					Interpreter.IsAuto = false;
					status = new[] { "Auto processing disabled." };
					return true;
				}
				if (contents[2] == "toggle")
				{
					Interpreter.IsAuto = !Interpreter.IsAuto;
					status = new[] { $"Auto processing {(Interpreter.IsAuto ? "enabled" : "disabled")}." };
					return true;
				}
				errorMsg = "Invalid syntax: invalid argument for #pragma auto directive.";
				return false;
			}
			if (pragma is "reflect")
			{
				if (contents.Length != 3)
				{
					errorMsg = "Invalid syntax: #pragma reflect syntax must have one argument.";
					return false;
				}
				if (contents[2] == "enable")
				{
					ManualQuery.IsReflect = true;
					status = new[] { "Reflector enabled." };
					return true;
				}
				if (contents[2] == "disable")
				{
					ManualQuery.IsReflect = false;
					status = new[] { "Reflector disabled." };
					return true;
				}
				if (contents[2] == "toggle")
				{
					ManualQuery.IsReflect = !ManualQuery.IsReflect;
					status = new[] { $"Reflector {(ManualQuery.IsReflect ? "enabled" : "disabled")}." };
					return true;
				}
				errorMsg = "Invalid syntax: invalid argument for #pragma reflect directive.";
				return false;
			}
			if (pragma is "easyarray")
			{
				if (contents.Length != 3)
				{
					errorMsg = "Invalid syntax: #pragma easyarray syntax must have one argument.";
					return false;
				}
				if (contents[2] == "enable")
				{
					Interpreter.EasyArrayInitializer = true;
					status = new[] { "Easy array enabled." };
					return true;
				}
				if (contents[2] == "disable")
				{
					Interpreter.EasyArrayInitializer = false;
					status = new[] { "Easy array disabled." };
					return true;
				}
				if (contents[2] == "toggle")
				{
					Interpreter.EasyArrayInitializer = !Interpreter.EasyArrayInitializer;
					status = new[] { $"Easy array {(Interpreter.EasyArrayInitializer ? "enabled" : "disabled")}." };
					return true;
				}
				errorMsg = "Invalid syntax: invalid argument for #pragma easyarray directive.";
				return false;
			}
			errorMsg = "Invalid syntax: invalid argument for #pragma monitor directive.";
			return false;
		}

		if (symbol is "ifdef")
		{
			status = new[] { ModuleManager.ContentNames.Contains(contents.At(1)) ? "TRUE" : "FALSE" };
			return true;
		}

		if (symbol is "ifndef")
		{
			status = new[] { !ModuleManager.ContentNames.Contains(contents.At(1)) ? "TRUE" : "FALSE" };
			return true;
		}

		if (symbol is "include")
		{
			if (ModuleManager.Include(contents.At(1) ?? string.Empty))
			{
				status = new[] { $"Successfully included {(contents.At(1) == "$ALL" ? "all modules" : $"module {contents.At(1)}")}." };
				return true;
			}
			errorMsg = $"No includable module {contents.At(1)} found.";
			return false;
		}


		if (symbol is "exclude")
		{
			if (ModuleManager.Exclude(contents.At(1) ?? string.Empty))
			{
				status = new[] { $"Successfully excluded {(contents.At(1) == "$ALL" ? "all modules" : $"module {contents.At(1)}")}." };
				return true;
			}
			errorMsg = $"No excludable module {contents.At(1)} found.";
			return false;
		}

		if (symbol is "delete")
		{
			if (ModuleManager.Delete(contents.At(1) ?? string.Empty, out var deleteStatus))
			{
				status = new[] { deleteStatus };
				return true;
			}
			errorMsg = deleteStatus;
			return false;
		}

		if (symbol is "undef")
		{
			if (contents.Length != 2)
			{
				errorMsg = "Invalid syntax: #undef syntax must have one argument.";
				return false;
			}
			if (contents[1] == "$ALL")
			{
				ModuleManager.UserDefined.Macros.Clear();
				ModuleManager.UserDefined.Symbols.Clear();
				status = new[] { "Cleared up the USER_DEFINED Macro Dictionary." };
				return true;
			}
			var count = ModuleManager.UserDefined.Macros.RemoveAll(m => m.Name == contents[1]);
			count += ModuleManager.UserDefined.Symbols.RemoveAll(s => s == contents[1]);
			if (count < 1)
			{
				errorMsg = $"Specified macro '{contents[1]}' does not exist in the USER_DEFINED module.";
				return false;
			}
			status = new[] { $"Successfully removed macro '{contents[1]}' from the dictionary." };
			return true;
		}

		if (symbol is "define")
		{

			var match = Module.DefineFunctionLikeMacroRegex().Match(input);
			if (match.Success)
			{
				var functionLikeMacro = new FunctionLikeMacro
				{
					Name = match.Groups["name"].Value,
					Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
					Body = match.Groups["body"].Value,
					ModuleName = moduleName
				};
				ModuleManager.UserDefined.Macros.Add(functionLikeMacro);
				status = new[] { $"Successfully added macro '{match.Groups["name"]}' to the dictionary." };
				return true;
			}

			if (contents.Length < 2)
			{
				errorMsg = "Invalid syntax: #define syntax must have more than one arguments.";
				return false;
			}

			if (contents.Length == 2)
			{
				ModuleManager.UserDefined.Symbols.Add(contents[1]);
				status = new[] { $"Successfully added symbol '{contents[1]}' to the dictionary." };
				return true;
			}

			var groups = Module.DefineObjectLikeMacroRegex().Match(input).Groups;
			var objectLikeMacro = new ObjectLikeMacro
			{
				Name = groups["key"].Value,
				Body = groups["value"].Value,
				ModuleName = moduleName
			};
			ModuleManager.UserDefined.Macros.Add(objectLikeMacro);

			status = new[] { $"Successfully added macro '{contents[1]}' to the dictionary." };
			return true;
		}

		errorMsg = $"Invalid directive: {input}";
		return false;
	}
}
