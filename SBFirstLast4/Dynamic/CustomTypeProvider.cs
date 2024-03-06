using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic;

public class CustomTypeProvider : DefaultDynamicLinqCustomTypeProvider, IDynamicLinkCustomTypeProvider
{
	private static readonly Type[] BuiltInTypes =
	{
		typeof(object), typeof(string), typeof(bool), typeof(byte),
		typeof(char), typeof(DateTime), typeof(DateTimeOffset), typeof(decimal),
		typeof(double), typeof(Guid), typeof(short), typeof(TimeSpan),
		typeof(int), typeof(long), typeof(sbyte), typeof(float),
		typeof(Half), typeof(ushort), typeof(uint), typeof(ulong)
	};

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
			["nint"] = typeof(nint),
			["nuint"] = typeof(nuint),
			["ushort"] = typeof(ushort),
			["uint"] = typeof(uint),
			["ulong"] = typeof(ulong)
		};
		var arrays = typeMapBase.ToDictionary(kv => $"{kv.Key}[]", kv => kv.Value.MakeArrayType());

		return typeMapBase.Concat(arrays).ToDictionary();
	}

	private Dictionary<Type, List<MethodInfo>>? _cachedExtensionMethods;

	public CustomTypeProvider(bool cacheCustomTypes = true) : base(cacheCustomTypes) { }

	public override HashSet<Type> GetCustomTypes()
	{
		var types = base.GetCustomTypes();
		types = types
				.Concat(typeof(Random).Assembly.GetTypes())
				.Concat(typeof(Regex).Assembly.GetTypes())
				.Concat(typeof(Enumerable).Assembly.GetTypes())
				.Concat(typeof(StringBuilder).Assembly.GetTypes())
				.Concat(Record.Types)
				.ToHashSet();

		return types;
	}

	internal HashSet<Type> GetAllTypes() => BuiltInTypes.Concat(GetCustomTypes()).ToHashSet();

	internal Type GetTypeByName(string typeStr) => BuiltInTypeMap.TryGetValue(typeStr, out var value) 
												? value
												: ResolveTypeBySimpleName(typeStr)
												?? ResolveType(typeStr)
												?? typeof(void);

	internal Dictionary<Type, List<MethodInfo>> GetAllExtensionMethods()
	{
		_cachedExtensionMethods ??= GetExtensionMethodsCore();

		return _cachedExtensionMethods;
	}

	private Dictionary<Type, List<MethodInfo>> GetExtensionMethodsCore()
	{
		var customTypes = BuiltInTypes.Concat(GetCustomTypes()).ToHashSet();

		var list = new List<(Type, MethodInfo)>();
		foreach (Type item in customTypes)
		{
			item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			 .Where(x => x.IsDefined(typeof(ExtensionAttribute), false))
			 .ToList()
			 .ForEach(x => list.Add((x.GetParameters()[0].ParameterType, x)));
		}

		return list
			.GroupBy(x => x.Item1, x => x.Item2)
			.ToDictionary(key => key.Key, methods => methods.ToList());
	}
}
