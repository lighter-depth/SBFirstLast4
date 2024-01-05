using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic;

[DynamicLinqType]
public static class WideVariable
{
	public static readonly Dictionary<string, dynamic> Variables = new();

	public static dynamic GetValue(string name) => Variables[name];

	public static string SetValue(string name, dynamic value)
	{
		Variables[name] = value;
		return string.Empty;
	}

	internal static string GetFormattedString(string name) => $"\"{Variables[name].GetType().FullName}\"({nameof(WideVariable)}.{nameof(GetValue)}(\"{name}\"))";
}
