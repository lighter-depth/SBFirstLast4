using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;
using VariableElement = System.Collections.Generic.KeyValuePair<string, SBFirstLast4.Dynamic.Variable<dynamic?>>;

namespace SBFirstLast4.Dynamic;

[DynamicLinqType]
public static class WideVariable
{
	private static readonly Dictionary<string, Variable<dynamic?>> Variables = [];

	public static string DefineValue(string name, dynamic? value, bool isReadOnly, bool isAssignable)
	{
		if (Variables.TryGetValue(name, out var defined) && !defined.IsAssignable)
			throw new DefinitionStaticViolationException($"Variable '{name}' is definition static.");

		Variables[name] = new(name, value, isReadOnly, isAssignable);

		return string.Empty;
	}

	public static dynamic? GetValue(string name) => Variables.TryGetValue(name, out var value) ? value.Value : null;

	public static string SetValue(string name, object? value)
	{
		if (Variables.TryGetValue(name, out var variable))
		{
			variable.Value = value;
			return string.Empty;
		}
		Variables[name] = new(name, value, false, true);
		return string.Empty;
	}

	public static bool RemoveValue(string name) => Variables.Remove(name);

	public static IEnumerable<VariableElement> Where(Func<VariableElement, bool> predicate)
		=> Variables.Where(predicate);

	public static void Increment(string name) => Variables[name].Value = Variables[name].Value + 1;

	public static object? IncrementAndGetValue(string name)
	{
		Increment(name);
		return GetValue(name);
	}

	public static void Decrement(string name) => Variables[name].Value = Variables[name].Value - 1;

	public static object? DecrementAndGetValue(string name)
	{
		Decrement(name);
		return GetValue(name);
	}


	private static string GetString(string name, string functionName)
	{
		var typename = GetValue(name)?.GetType().FullName;
		if (string.IsNullOrEmpty(typename))
			return "null";

		return $"(\"{typename}\"({nameof(WideVariable)}.{functionName}(\"{name}\")))";
	}

	internal static string GetFormattedString(string name) => GetString(name, nameof(GetValue));

	internal static string GetIncrementString(string name) => GetString(name, nameof(IncrementAndGetValue));

	internal static string GetDecrementString(string name) => GetString(name, nameof(DecrementAndGetValue));
}

internal static partial class WideVariableRegex
{
	internal static List<(Regex, AssignmentType)> Assignments =>
	[
		(AddAssign(), AssignmentType.Add),
		(SubtractAssign(), AssignmentType.Subtract),
		(MultiplyAssign(), AssignmentType.Multiply),
		(DivideAssign(), AssignmentType.Divide),
		(ModulusAssign(), AssignmentType.Modulus),
		(AndAssign(), AssignmentType.And),
		(OrAssign(), AssignmentType.Or),
		(XorAssign(), AssignmentType.Xor),
		(LeftShiftAssign(), AssignmentType.LeftShift),
		(RightShiftAssign(), AssignmentType.RightShift),
		(CoalsceAssign(), AssignmentType.Coalsce)
	];

	private const string VariablePattern = "&(?<name>[A-Z_a-z][0-9A-Z_a-z]*)";

	[GeneratedRegex(VariablePattern)]
	internal static partial Regex Reference();

	[GeneratedRegex(@"\^(?<name>[A-Z_a-z][0-9A-Z_a-z]*)")]
	internal static partial Regex InternalReference();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*$")]
	internal static partial Regex SingleReference();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*=(?<expr>[^=].*)$")]
	internal static partial Regex Declaration();

	[GeneratedRegex($@"^\s*(?<attributes>(?:var|let|mutable|const))\s*{VariablePattern}\s*=(?<expr>[^=].*)$")]
	internal static partial Regex Definition();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*(?:\.|\[).*=(?<expr>[^=].*)$")]
	internal static partial Regex MemberAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*=\s*%\s*{{(?<expr>.*)}}\s*$")]
	internal static partial Regex Hash();

	[GeneratedRegex(@"%\s*{(?<expr>.*?)}\s*(?:[()\[\].\\]|$)")]
	internal static partial Regex HashExpression();

	[GeneratedRegex(@"%\s*{")]
	internal static partial Regex HashStart();

	[GeneratedRegex(@"(?<key>.*?)\s*=\s*(?<value>.*?)(?:,|$)")]
	internal static partial Regex HashInitializer();

	[GeneratedRegex(@"(?<key>.*?)\s*=\s*(?<value>.*?)(?:}\s*,|}\s*$)")]
	internal static partial Regex HashArrayInitializer();

	[GeneratedRegex($@"^\s*delete\s*{VariablePattern}")]
	internal static partial Regex Deletion();

	[GeneratedRegex($@"{VariablePattern}\+\+|\+\+{VariablePattern}")]
	internal static partial Regex Increment();

	[GeneratedRegex($@"{VariablePattern}\-\-|\-\-{VariablePattern}")]
	internal static partial Regex Decrement();

	[GeneratedRegex($@"^\s*{VariablePattern}\+\+\s*$|^\s*\+\+{VariablePattern}\s*$")]
	internal static partial Regex IncrementStatement();

	[GeneratedRegex($@"^\s*{VariablePattern}\-\-\s*$|^\s*\-\-{VariablePattern}\s*$")]
	internal static partial Regex DecrementStatement();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\+=(?<expr>.*)$")]
	internal static partial Regex AddAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\-=(?<expr>.*)$")]
	internal static partial Regex SubtractAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\*=(?<expr>.*)$")]
	internal static partial Regex MultiplyAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\/=(?<expr>.*)$")]
	internal static partial Regex DivideAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\%=(?<expr>.*)$")]
	internal static partial Regex ModulusAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\&=(?<expr>.*)$")]
	internal static partial Regex AndAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\|=(?<expr>.*)$")]
	internal static partial Regex OrAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\^=(?<expr>.*)$")]
	internal static partial Regex XorAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*<<=(?<expr>.*)$")]
	internal static partial Regex LeftShiftAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*>>=(?<expr>.*)$")]
	internal static partial Regex RightShiftAssign();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*\?\?=(?<expr>.*)$")]
	internal static partial Regex CoalsceAssign();
}

internal enum AssignmentType
{
	Add, Subtract, Multiply, Divide, Modulus, And, Or, Xor, LeftShift, RightShift, Coalsce
}
