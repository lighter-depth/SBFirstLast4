using SBFirstLast4.Pages;
using System.Text.RegularExpressions;

namespace SBFirstLast4.Dynamic;

internal class ModuleReader
{
	private readonly string _moduleName;
	private readonly Stack<bool> _isDisabled;
	private readonly Stack<bool> _ifDefStack = new();

	private QueryContext CurrentContext => _contextStack.Peek();

	private readonly Stack<QueryContext> _contextStack = new() { QueryContext.Module };

	private readonly Stack<ValueTuple> _scriptContextTrackingStack = new();

	private readonly List<string> _newlineBuffer = new();

	private Procedure? _currentProcedure;

	internal List<string> Symbols { get; private set; } = new();

	internal List<Macro> Macros { get; private set; } = new();

	internal List<Macro> Ephemerals { get; private set; } = new();

	internal List<Procedure> Procedures { get; private set; } = new();

	internal List<string> InitStatements { get; private set; } = new();

	internal List<string> StaticStatements { get; private set; } = new();

	private readonly List<Macro> Transients = new();

	private readonly List<string> OmitSymbols = new();

	public ModuleReader(string moduleName)
	{
		_moduleName = moduleName;
		_isDisabled = new();
		_isDisabled.Push(false);
	}


	internal ModuleReader ReadContents(string[] contents)
	{
		foreach (var content in contents)
			Run(content);


		foreach (var macro in Macros)
		{
			var body = macro.Body;

			foreach (var transient in Transients)
				body = Transient.Expand(body, transient);

			macro.Body = body;
		}

		foreach (var proc in Procedures)
			foreach (var transient in Transients)
				proc.ExpandTransient(transient);

		foreach (var transient in Transients)
		{
			InitStatements = InitStatements.Select(s => Transient.Expand(s, transient)).ToList();
			StaticStatements = StaticStatements.Select(s => Transient.Expand(s, transient)).ToList();
		}

		return this;
	}

	private void Run(string input)
	{
		var inputTrim = input.Trim();

		if (inputTrim.EndsWith('\\'))
		{
			_newlineBuffer.Add(inputTrim[..^1]);
			return;
		}

		inputTrim = _newlineBuffer.Append(inputTrim).StringJoin(string.Empty);
		_newlineBuffer.Clear();

		if (CurrentContext is QueryContext.Init)
		{
			ProcessScriptContext(inputTrim, QueryContext.Init);
			return;
		}

		if (CurrentContext is QueryContext.Static)
		{
			ProcessScriptContext(inputTrim, QueryContext.Static);
			return;
		}


		if (!_isDisabled.Peek() && inputTrim.StartsWith('.') && QueryContexts.ModuleContextSpecifiers.Contains(inputTrim.Split().At(0)))
		{
			SwitchContext(inputTrim[1..]);
			return;
		}

		if (CurrentContext is QueryContext.Module)
			ProcessDirective(inputTrim);

		if (CurrentContext is QueryContext.NamedProcedure)
			_currentProcedure?.Push(inputTrim);
	}

	private void SwitchContext(string contextStr)
	{
		if (contextStr == "end" && _contextStack.Count > 1)
		{
			endProcedure();
			_contextStack.Pop();
			return;
		}
		if (contextStr == "default")
		{
			endProcedure();
			_contextStack.Clear();
			_contextStack.Push(QueryContext.Module);
			return;
		}
		if (contextStr == "module" && CurrentContext != QueryContext.Module)
		{
			endProcedure();
			_contextStack.Push(QueryContext.Module);
			return;
		}

		if (contextStr == "init" && CurrentContext != QueryContext.Init)
		{
			endProcedure();
			_contextStack.Push(QueryContext.Init);
			return;
		}

		if (contextStr == "static" && CurrentContext != QueryContext.Static)
		{
			endProcedure();
			_contextStack.Push(QueryContext.Static);
			return;
		}

		if (contextStr.Split().At(0) == "proc" && CurrentContext != QueryContext.NamedProcedure)
		{
			var procDeclaration = contextStr.Skip(4).SkipWhile(char.IsWhiteSpace).StringJoin();
			var procNameEnd = procDeclaration.IndexOf('!');
			var procName = procNameEnd == -1 ? null : procDeclaration.Take(..procNameEnd).StringJoin();

			if (procName is null)
				return;

			var rparen = procDeclaration.LastIndexOf(')');
			var parameterText = rparen > procNameEnd + 1 ? procDeclaration.Take((procNameEnd + 2)..rparen).StringJoin() : string.Empty;
			var parameters = parameterText
							.Split(',')
							.Select(s => s.Trim())
							.Where(s => s.StartsWith('&') || s.StartsWith('^'))
							.ToList();


			_currentProcedure = new(procName, parameters, _moduleName, Procedure.DiscardAgent, Procedure.DiscardBuffer, Procedure.DiscardSetTranslated, Procedure.DiscardUpdate, ManualQuery.Cancellation.Token);
			_contextStack.Push(QueryContext.NamedProcedure);
		}

		void endProcedure()
		{
			if (CurrentContext == QueryContext.NamedProcedure && _currentProcedure is not null)
				Procedures.Add(_currentProcedure.Clone());
		}
	}

	private void ProcessScriptContext(string input, QueryContext context)
	{
		if (QueryContexts.ScriptContextSpecifiers.Contains(input.Split().At(0)))
		{
			if (input == ".end")
			{
				if (_scriptContextTrackingStack.Count < 1)
				{
					_contextStack.Pop();
					return;
				}
				_scriptContextTrackingStack.Pop();
			}
			else
				_scriptContextTrackingStack.Push(default);
		}
		if (context is QueryContext.Init)
			InitStatements.Add(input);

		if (context is QueryContext.Static)
			StaticStatements.Add(input);
	}

	private void ProcessDirective(string directiveStr)
	{
		var directive = directiveStr.Split();
		switch (directive.At(0))
		{
			case "#define":
				{
					if (_isDisabled.Peek() || directive.Length < 2)
						break;

					if (directive.Length == 2)
					{
						Symbols.Add(directive[1]);
						break;
					}

					var match = ModuleRegex.DefineFunctionLikeMacro().Match(directiveStr);
					if (match.Success)
					{
						var functionLikeMacro = new FunctionLikeMacro
						{
							Name = match.Groups["name"].Value,
							Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
							Body = match.Groups["body"].Value,
							ModuleName = _moduleName
						};
						Macros.Add(functionLikeMacro);
						break;
					}

					var groups = ModuleRegex.DefineObjectLikeMacro().Match(directiveStr).Groups;
					var objectLikeMacro = new ObjectLikeMacro
					{
						Name = groups["key"].Value,
						Body = groups["value"].Value,
						ModuleName = _moduleName
					};
					Macros.Add(objectLikeMacro);
					break;
				}
			case "#undef":
				{
					if (_isDisabled.Peek() || directive.Length < 2)
						break;

					var symbol = directive[1];
					OmitSymbols.Add(symbol);
					Symbols.RemoveAll(s => s == symbol);
					Macros.RemoveAll(m => m.Name == symbol);
					break;
				}
			case "#ephemeral":
				{
					if (_isDisabled.Peek() || directive.Length < 3)
						break;

					var match = ModuleRegex.EphemeralFunctionLikeMacro().Match(directiveStr);
					if (match.Success)
					{
						var functionLikeMacro = new FunctionLikeMacro
						{
							Name = match.Groups["name"].Value,
							Parameters = match.Groups["parameters"].Value.Split(',').Select(p => p.Trim()).ToList(),
							Body = match.Groups["body"].Value,
							ModuleName = _moduleName
						};
						Ephemerals.Add(functionLikeMacro);
						break;
					}

					var groups = ModuleRegex.EphemeralObjectLikeMacro().Match(directiveStr).Groups;
					var objectLikeMacro = new ObjectLikeMacro
					{
						Name = groups["key"].Value,
						Body = groups["value"].Value,
						ModuleName = _moduleName
					};
					Ephemerals.Add(objectLikeMacro);
					break;
				}
			case "#evaporate":
				{
					if (_isDisabled.Peek() || directive.Length < 2)
						break;

					var symbol = directive[1];
					OmitSymbols.Add(symbol);
					Symbols.RemoveAll(s => s == symbol);
					Ephemerals.RemoveAll(m => m.Name == symbol);
					break;
				}
			case "#transient":
				{
					if (_isDisabled.Peek() || directive.Length < 2)
						break;

					if (directive.Length == 2)
					{
						Symbols.Add(directive[1]);
						break;
					}

					var match = ModuleRegex.TransientFunctionLikeMacro().Match(directiveStr);
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

					var groups = ModuleRegex.TransientObjectLikeMacro().Match(directiveStr).Groups;
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
					_ifDefStack.Push(isDefined);
					_isDisabled.Push(_isDisabled.Peek() || !isDefined);
					break;
				}

			case "#ifndef":
				{
					var isNotDefined = !ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					_ifDefStack.Push(isNotDefined);
					_isDisabled.Push(_isDisabled.Peek() || !isNotDefined);
					break;
				}

			case "#elifdef":
				{
					if (_ifDefStack.Count <= 0)
						break;

					var prevResult = _ifDefStack.Pop();
					var isDefined = ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					_ifDefStack.Push(isDefined);
					_isDisabled.Pop();
					_isDisabled.Push(_isDisabled.Peek() || prevResult || !isDefined);
					break;
				}

			case "#elifndef":
				{
					if (_ifDefStack.Count <= 0)
						break;

					var prevResult = _ifDefStack.Pop();
					var isNotDefined = !ModuleManager.Symbols.Concat(Symbols).Except(OmitSymbols).Contains(directive.At(1));
					_ifDefStack.Push(isNotDefined);
					_isDisabled.Pop();
					_isDisabled.Push(_isDisabled.Peek() || prevResult || !isNotDefined);
					break;
				}
			case "#else":
				{
					if (_ifDefStack.Count <= 0)
						break;

					var prevResult = _ifDefStack.Pop();
					_ifDefStack.Push(!prevResult);
					_isDisabled.Pop();
					_isDisabled.Push(_isDisabled.Peek() || prevResult);
					break;
				}
			case "#endif":
				{
					if (_ifDefStack.Count <= 0)
						break;

					_ifDefStack.Pop();
					_isDisabled.Pop();
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
