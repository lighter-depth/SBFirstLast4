﻿using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SBFirstLast4.Dynamic;

[DynamicLinqType]
public static class WideVariable
{
	public static readonly Dictionary<string, dynamic?> Variables = new();

	public static object? GetValue(string name) => Variables[name];

	public static string SetValue(string name, object? value)
	{
		Variables[name] = value;
		return string.Empty;
	}

	public static void Increment(string name) => Variables[name]++;

	public static object? IncrementAndGetValue(string name)
	{
		Increment(name);
		return GetValue(name);
	}

	public static void Decrement(string name) => Variables[name]--;

	public static object? DecrementAndGetValue(string name)
	{
		Decrement(name);
		return GetValue(name);
	}


	private static string GetString(string name, string functionName) 
		=> Variables[name]?.GetType().FullName is string typename && !string.IsNullOrEmpty(typename)
		? $"\"{typename}\"({nameof(WideVariable)}.{functionName}(\"{name}\"))"
		: "null";

	internal static string GetFormattedString(string name) => GetString(name, nameof(GetValue));

	internal static string GetIncrementString(string name) => GetString(name, nameof(IncrementAndGetValue));

	internal static string GetDecrementString(string name) => GetString(name, nameof(DecrementAndGetValue));
}

internal static partial class WideVariableRegex
{
	internal static List<(Regex, AssignmentType)> Assignments => new()
	{
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
		(CoarseAssign(), AssignmentType.Coarse)
	};

	// lang=regex
	private const string VariablePattern = "&(?<name>[A-Za-z][0-9A-Z_a-z]*)";

	[GeneratedRegex(VariablePattern)]
	internal static partial Regex Reference();

	[GeneratedRegex($@"^\s*{VariablePattern}\s*=(?<expr>.*)$")]
	internal static partial Regex Declaration();

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
	internal static partial Regex CoarseAssign();
}

internal enum AssignmentType
{
	Add, Subtract, Multiply, Divide, Modulus, And, Or, Xor, LeftShift, RightShift, Coarse
}
