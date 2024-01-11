using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Text;

namespace SBFirstLast4.Dynamic;

[DynamicLinqType]
public static class Record
{
	internal static readonly AssemblyBuilder AssemblyBuilder
		= AssemblyBuilder.DefineDynamicAssembly(new("SBFirstLast4Dynamic"), AssemblyBuilderAccess.Run);

	internal static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule("SBFirstLast4Dynamic");

	internal static readonly List<Type> Types = new();

	internal static readonly List<string> TypeNames = new();

	internal static readonly Dictionary<string, Type> BuiltInTypeMap = GetTypeMap();

	private static Dictionary<string, Type> GetTypeMap()
	{
		var typeMapBase = new Dictionary<string, Type>
		{
			["object"] = typeof(object),
			["string"] = typeof(string),
			["bool"] = typeof(bool),
			["byte"] = typeof(byte),
			["char"] = typeof(char),
			["decimal"] = typeof(decimal),
			["double"] = typeof(double),
			["short"] = typeof(short),
			["int"] = typeof(int),
			["long"] = typeof(long),
			["sbyte"] = typeof(sbyte),
			["float"] = typeof(float),
			["half"] = typeof(Half),
			["ushort"] = typeof(ushort),
			["uint"] = typeof(uint),
			["ulong"] = typeof(ulong)
		};
		var arrays = typeMapBase
					.Select(kv => ($"{kv.Key}[]", kv.Value.MakeArrayType()))
					.ToDictionary(t => t.Item1, t => t.Item2);

		return typeMapBase.Concat(arrays).ToDictionary(kv => kv.Key, kv => kv.Value);
	}


	public static string Emit(string recordName, string expression)
	{
		var builder = ModuleBuilder.DefineType(recordName);

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

			var type = BuiltInTypeMap.TryGetValue(typeStr, out var value) 
						? value
						: provider.ResolveTypeBySimpleName(typeStr) 
						?? provider.ResolveType(typeStr)
						?? typeof(object);

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
		TypeNames.Add(recordName);

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
}
