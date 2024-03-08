using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic;

[DynamicLinqType]
public static class Record
{
	internal static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("SBFirstLast4Dynamic"), AssemblyBuilderAccess.Run);

	internal static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule("SBFirstLast4Dynamic");

	internal static readonly List<Type> Types = new();

	private const string Namespace = "SBFirstLast4Dynamic";

	public static string Emit(string recordName, string expression)
	{
		var builder = ModuleBuilder.DefineType($"{Namespace}.{recordName}");

		var parameters = expression.Split(',').Select(s => s.Trim());

		var paramTypes = new List<Type>();
		var fields = new List<FieldBuilder>();

		var provider = new CustomTypeProvider();

		foreach (var param in parameters)
		{
			var paramMatch = RecordRegex.StatementParameter().Match(param);
			if (!paramMatch.Success)
				throw new FormatException("Invalid format for record parameter specification");

			var typeStr = paramMatch.Groups["type"].Value;
			var name = paramMatch.Groups["name"].Value;

			var type = provider.GetTypeByName(typeStr);

			paramTypes.Add(type);

			fields.Add(builder.DefineField(name, type, FieldAttributes.Public));
		}

		var ctor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, paramTypes.ToArray());

		DefineConstructor(ctor.GetILGenerator(), fields);


		var printMembers = builder.DefineMethod("PrintMembers", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), new[] { typeof(StringBuilder) });
		DefinePrintMembers(printMembers.GetILGenerator(), fields);

		var toString = builder.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), null);
		DefineToString(toString.GetILGenerator(), builder.Name, printMembers);


		Types.Add(builder.CreateType());

		return string.Empty;
	}

	public static string EmitEnum(string enumName, string[] enumMembers)
	{
		var builder = ModuleBuilder.DefineEnum($"{Namespace}.{enumName}", TypeAttributes.Public, typeof(int));

		builder.SetCustomAttribute(new(typeof(FlagsAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object?>()));

		var value = 0;

		foreach (var i in enumMembers)
		{
			if(i.Split('=') is var split && split.Length == 2)
			{
				value = int.Parse(split[1].Trim());
				builder.DefineLiteral(split[0].Trim(), value);
				value++;
				continue;
			}
			builder.DefineLiteral(i, value);
			value++;
		}
		Types.Add(builder.CreateType());
		return string.Empty;
	}

	private static void DefineConstructor(ILGenerator il, IList<FieldBuilder> fields)
	{
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

		foreach (var (field, i) in fields.WithIndex())
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg, i + 1);
			il.Emit(OpCodes.Stfld, field);
		}

		il.Emit(OpCodes.Ret);
	}

	private static void DefinePrintMembers(ILGenerator il, IList<FieldBuilder> fields)
	{
		var appendString = typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!;
		var appendObject = typeof(StringBuilder).GetMethod("Append", new[] { typeof(object) })!;

		var sb = il.DeclareLocal(typeof(StringBuilder));

		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Stloc, sb);

		for (var i = 0; i < fields.Count; i++)
		{
			if (i > 0)
			{
				il.Emit(OpCodes.Ldloc, sb);
				il.Emit(OpCodes.Ldstr, ", ");
				il.Emit(OpCodes.Callvirt, appendString);
				il.Emit(OpCodes.Pop);
			}

			il.Emit(OpCodes.Ldloc, sb);
			il.Emit(OpCodes.Ldstr, fields[i].Name);
			il.Emit(OpCodes.Callvirt, appendString);
			il.Emit(OpCodes.Pop);

			il.Emit(OpCodes.Ldloc, sb);
			il.Emit(OpCodes.Ldstr, " = ");
			il.Emit(OpCodes.Callvirt, appendString);
			il.Emit(OpCodes.Pop);

			il.Emit(OpCodes.Ldloc, sb);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, fields[i]);

			if (fields[i].FieldType.IsValueType)
				il.Emit(OpCodes.Box, fields[i].FieldType);

			il.Emit(OpCodes.Callvirt, appendObject);
			il.Emit(OpCodes.Pop);
		}

		il.Emit(OpCodes.Ldc_I4_1);
		il.Emit(OpCodes.Ret);
	}

	private static void DefineToString(ILGenerator il, string typeName, MethodInfo printMembers)
	{
		var appendString = typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) })!;
		var endif = il.DefineLabel();
		var sb = il.DeclareLocal(typeof(StringBuilder));
		il.Emit(OpCodes.Newobj, typeof(StringBuilder).GetConstructor(Type.EmptyTypes)!);
		il.Emit(OpCodes.Stloc, sb);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Ldstr, typeName);
		il.Emit(OpCodes.Call, appendString);
		il.Emit(OpCodes.Pop);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Ldstr, " { ");
		il.Emit(OpCodes.Call, appendString);
		il.Emit(OpCodes.Pop);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Call, printMembers);
		il.Emit(OpCodes.Brfalse, endif);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Ldstr, " ");
		il.Emit(OpCodes.Call, appendString);
		il.Emit(OpCodes.Pop);
		il.MarkLabel(endif);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Ldstr, "}");
		il.Emit(OpCodes.Call, appendString);
		il.Emit(OpCodes.Pop);
		il.Emit(OpCodes.Ldloc, sb);
		il.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString", Type.EmptyTypes)!);
		il.Emit(OpCodes.Ret);
	}
}

internal static partial class RecordRegex
{
	[GeneratedRegex(@"^\s*record\s+(?<name>[A-Za-z][0-9A-Z_a-z]*)\s*\((?<expr>[^\)]*)\)\s*$")]
	internal static partial Regex Statement();

	[GeneratedRegex(@"^\s*(?<type>.+)\s+(?<name>[A-Za-z][0-9A-Z_a-z]*)\s*$")]
	internal static partial Regex StatementParameter();

	[GeneratedRegex(@"^\s*enum\s+(?<name>[A-Za-z][0-9A-Z_a-z]*)\s*{")]
	internal static partial Regex Enum();
}
