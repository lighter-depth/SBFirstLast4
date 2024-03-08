using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace SBFirstLast4.Dynamic;

public class PostcallVisitor : SBProcLangBaseVisitor<Task<string?>?>
{
	private string _source;

	private readonly CustomTypeProvider _customTypeProvider = new();

	private static int PostCallId = 0;

	private Exception? @throw;

	private static readonly Dictionary<ConstructorSignature, ConstructorInfo> _ctorCache = new();

	private static readonly Dictionary<MethodSignature, (MethodInfo MethodInfo, bool IsExtension)> _addCache = new();

	private static readonly Dictionary<MethodSignature, (MethodInfo MethodInfo, bool IsExtension)> _methodCache = new();

#if DEBUG
	internal static void ClearCache() => _methodCache.Clear();
#endif

	private static readonly Dictionary<string, Type> _typeCache = new();

	internal PostcallVisitor(string source) => _source = source;

	protected override Task<string?>? DefaultResult => Task.FromResult("DEFAULT")!;

	public override async Task<string?> VisitExpr([NotNull] SBProcLangParser.ExprContext context)
	{
		var task = VisitChildren(context);

		if (task is not null)
			await task;

		if (@throw is not null)
			throw @throw;

		return _source;
	}

	public override async Task<string?> VisitPostcall([NotNull] SBProcLangParser.PostcallContext context)
	{
		try
		{
			var targetStr = context.children[0].GetText();

			var methodCall = context.methodCall();

			object? result = null;

			if (targetStr == "new")
				result = await NewPostcall(methodCall);

			else if (PostcallRegex.GenericTypeName().IsMatch(targetStr))
			{
				var type = GetGenericTypeArgument(targetStr).At(0) ?? typeof(void);
				result = await StaticPostcall(type, methodCall);
			}
			else
			{
				var target = await QueryRunner.EvaluateExpressionAsync(targetStr);

				result = await InstancePostcall(target, methodCall);
			}

			var pattern = Regex.Escape(context.children
				.Take(3)
				.Select(c => c.GetText())
				.StringJoin()
				.StringJoin(@"\s*"))
				.Replace(@"\\s\*", @"\s*");

			var match = Regex.Match(_source, pattern);
			if (!match.Success)
				return _source;

			var variableName = $"__postcall_{PostCallId}_generated";
			PostCallId++;
			WideVariable.SetValue(variableName, result);

			var sb = new StringBuilder(_source);
			sb.Remove(match.Index, match.Length);
			sb.Insert(match.Index, $"&{variableName}");
			_source = sb.ToString();

			return _source;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return null;
		}
	}

	public override async Task<string?> VisitObjectAccess([NotNull] SBProcLangParser.ObjectAccessContext context)
	{
		try
		{
			var factor = context.factor();

			await EvaluateMethods(factor.methodCall());

			if (factor.initCall() is { } initCall)
				await VisitInitCall(initCall);

			if (factor.tupleCall() is { } tupleCall)
				await VisitTupleCall(tupleCall);

			await EvaluateMethods(context.methodCall());

			if (context.postcallRest() is null)
				return _source;

			var targetStr = context.factor().GetText();
			var target = await QueryRunner.EvaluateExpressionAsync(targetStr);

			var postCall = context.postcallRest().methodCall();

			var result = await InstancePostcall(target, postCall);


			var pattern = Regex.Escape(context.children
				.Take(3)
				.Select(c => c.GetText())
				.StringJoin()
				.StringJoin(@"\s*"))
				.Replace(@"\\s\*", @"\s*");

			var match = Regex.Match(_source, pattern);
			if (!match.Success)
				return _source;

			var variableName = $"__postcall_{PostCallId}_generated";
			PostCallId++;
			WideVariable.SetValue(variableName, result);

			var sb = new StringBuilder(_source);
			sb.Remove(match.Index, match.Length);
			sb.Insert(match.Index, $"&{variableName}");
			_source = sb.ToString();

			return _source;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return null;
		}
	}

	public override async Task<string?> VisitInitCall([NotNull] SBProcLangParser.InitCallContext context)
	{
		try
		{
			var result = await InitPostcall(context);

			var pattern = Regex.Escape(context.children
				.Select(c => c.GetText())
				.StringJoin()
				.StringJoin(@"\s*"))
				.Replace(@"\\s\*", @"\s*");

			var match = Regex.Match(_source, pattern);
			if (!match.Success)
				return _source;

			var variableName = $"__postcall_{PostCallId}_generated";
			PostCallId++;
			WideVariable.SetValue(variableName, result);

			var sb = new StringBuilder(_source);
			sb.Remove(match.Index, match.Length);
			sb.Insert(match.Index, $"&{variableName}");
			_source = sb.ToString();

			return _source;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return null;
		}
	}

	public override async Task<string?> VisitTupleCall([NotNull] SBProcLangParser.TupleCallContext context)
	{
		try
		{
			var result = await TuplePostcall(context);


			var pattern = Regex.Escape(context.children
				.Select(c => c.GetText())
				.StringJoin()
				.StringJoin(@"\s*"))
				.Replace(@"\\s\*", @"\s*");

			var match = Regex.Match(_source, pattern);
			if (!match.Success)
				return _source;

			var variableName = $"__postcall_{PostCallId}_generated";
			PostCallId++;
			WideVariable.SetValue(variableName, result);

			var sb = new StringBuilder(_source);
			sb.Remove(match.Index, match.Length);
			sb.Insert(match.Index, $"&{variableName}");
			_source = sb.ToString();

			return _source;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return null;
		}
	}

	private async Task EvaluateMethods(SBProcLangParser.MethodCallContext? methodCall)
	{
		if (methodCall is null)
			return;

		var args = methodCall.children
			.Take((methodCall.methodOpen.TokenIndex - methodCall.methodName.TokenIndex)..(methodCall.methodClose.TokenIndex - methodCall.methodName.TokenIndex))
			.OfType<SBProcLangParser.ExprContext>()
			.ToArray();

		foreach (var arg in args)
			await VisitExpr(arg);
	}

	private async Task<object?> NewPostcall(SBProcLangParser.MethodCallContext methodCall)
	{
		try
		{
			var typeName = methodCall.children
				.Take(methodCall.methodOpen.TokenIndex - methodCall.methodName.TokenIndex)
				.StringJoin();

			if (typeName is null)
				return InvalidPostcall.Value;

			var type = GetTypeByName(typeName);

			var argStr = methodCall.children
				.Take((methodCall.methodOpen.TokenIndex - methodCall.methodName.TokenIndex)..(methodCall.methodClose.TokenIndex - methodCall.methodName.TokenIndex))
				.OfType<SBProcLangParser.ExprContext>()
				.Select(c => c.GetText())
				.ToArray();

			var args = new List<object?>();

			foreach (var i in argStr)
				args.Add(await QueryRunner.EvaluateExpressionAsync(i));

			var argTypes = args.Select(a => a.GetTypeOrDefault()).ToArray();

			var signature = new ConstructorSignature(type, argTypes);

			if (_ctorCache.TryGetValue(signature, out var cachedCtor))
				return cachedCtor.Invoke(args.ToArray());

			var ctor = type.GetConstructor(argTypes);

			if (ctor is null)
			{
				@throw = new NoSuchConstructorException($"Could not find a matching constructor '{signature}'");
				return InvalidPostcall.Value;
			}

			_ctorCache.Add(signature, ctor);

			var result = ctor.Invoke(args.ToArray());

			return result;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return InvalidPostcall.Value;
		}
	}

	private async Task<object?> InitPostcall(SBProcLangParser.InitCallContext initCall)
	{
		try
		{
			var start = initCall.initToken.TokenIndex;

			var typeName = initCall.children
				.Take(2..(initCall.initOpen.TokenIndex - start))
				.StringJoin();

			if (typeName is null)
				return InvalidPostcall.Value;

			var argStr = initCall.children
				.Take((initCall.initOpen.TokenIndex - start)..(initCall.initClose.TokenIndex - start))
				.OfType<SBProcLangParser.ExprContext>()
				.Select(c => c.GetText())
				.ToArray();

			var args = new List<object?>();

			foreach (var i in argStr)
				args.Add(await QueryRunner.EvaluateExpressionAsync(i));

			var type = GetTypeByName(typeName);

			if (type.IsArray)
			{
				var length = args.Count;
				var array = Array.CreateInstance(type.GetElementType()!, length);
				for (var i = 0; i < length; i++)
					array.SetValue(args[i], i);
				return array;
			}

			var argType = args.FirstOrDefault().GetTypeOrDefault();
			var argTypes = new[] { argType };

			var signature = new MethodSignature(type, "Add", null, argTypes);

			if (_addCache.TryGetValue(signature, out var cachedMethod))
			{
				var obj = Activator.CreateInstance(type);

				if (cachedMethod.IsExtension)
					foreach (var arg in args)
						cachedMethod.MethodInfo.Invoke(null, new[] { obj, arg });

				else
					foreach (var arg in args)
						cachedMethod.MethodInfo.Invoke(obj, new[] { arg });

				return obj;
			}

			var method = type.GetMethod("Add", argTypes);

			if (method is not null)
			{
				var obj = Activator.CreateInstance(type);
				foreach (var arg in args)
					method.Invoke(obj, new[] { arg });

				_addCache.Add(signature, (method, false));
				return obj;
			}


			var extensionDictionary = _customTypeProvider.GetAllExtensionMethods();


			var extensionMethods = extensionDictionary.SelectMany(kv => kv.Value).Where(m => m.Name == "Add" && IsAssignableToGeneric(type, m.GetParameters()[0].ParameterType)).ToArray();

			var extensionParamTypes = new[] { type }.Concat(argTypes).ToArray();

			var extensionMethod = extensionMethods
				.Where(m => m
				.GetParameterTypes()
				.SequenceEqual(extensionParamTypes, TypeEqualityComparer.Instance))
				.FirstOrDefault();

			if (extensionMethod is null)
			{
				var methodSignature = $"{type.Name}.Add({argTypes.Select(t => t.Name).StringJoin(", ")})";
				@throw = new NoSuchMethodException($"Could not find a matching method or an extension method '{methodSignature}'");
				return InvalidPostcall.Value;
			}

			var target = Activator.CreateInstance(type);

			if (!extensionMethod.IsGenericMethod)
			{
				foreach (var arg in args)
					extensionMethod.Invoke(null, new[] { target, arg });

				_addCache.Add(signature, (extensionMethod, true));
				return target;
			}

			var locations = GetTypeArgumentLocations(extensionMethod);

			if (locations.Contains(TypeArgumentLocation.Nowhere))
				return InvalidPostcall.Value;

			var castedParameters = extensionParamTypes
				.Zip(extensionMethod.GetParameterTypes())
				.Select(t => t.Second.IsGenericParameter ? t.First : t.Second.IsGenericType ? t.First.GetInterface(t.Second.Name) ?? t.First : t.Second)
				.ToArray();

			var genericTypeArgs = locations.Select(l => l.Access(castedParameters)).ToArray();

			var genericMethod = extensionMethod.MakeGenericMethod(genericTypeArgs);
			foreach (var arg in args)
				genericMethod.Invoke(null, new[] { target, arg });

			_addCache.Add(signature, (genericMethod, true));
			return target;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return InvalidPostcall.Value;
		}
	}

	private async Task<object?> TuplePostcall(SBProcLangParser.TupleCallContext tupleCall)
	{
		try
		{
			var start = tupleCall.tupleToken.TokenIndex;
			var argStr = tupleCall.children
				.Take((tupleCall.tupleOpen.TokenIndex - start)..(tupleCall.tupleClose.TokenIndex - start))
				.OfType<SBProcLangParser.ExprContext>()
				.Select(c => c.GetText())
				.ToArray();

			var args = new List<object?>();

			foreach (var i in argStr)
				args.Add(await QueryRunner.EvaluateExpressionAsync(i));

			var result = MakeTuple(args.ToArray());

			return result;
		}
		catch (Exception ex)
		{
			@throw = ex;
			return InvalidPostcall.Value;
		}
	}

	private async Task<object?> InstancePostcall(object? target, SBProcLangParser.MethodCallContext methodCall)
	{
		try
		{
			var methodName = methodCall.methodName.Text;

			if (methodName is null)
				return InvalidPostcall.Value;

			var type = target.GetTypeOrDefault();

			var argStr = methodCall.children
				.Take((methodCall.methodOpen.TokenIndex - methodCall.methodName.TokenIndex)..(methodCall.methodClose.TokenIndex - methodCall.methodName.TokenIndex))
				.OfType<SBProcLangParser.ExprContext>()
				.Select(c => c.GetText())
				.ToArray();

			var args = new List<object?>();

			foreach (var i in argStr)
				args.Add(await QueryRunner.EvaluateExpressionAsync(i));

			var argTypes = args.Select(a => a.GetTypeOrDefault()).ToArray();

			string? typeParameterText = null;
			if (methodCall.openAngle is not null)
			{
				typeParameterText = methodCall.children
					.Take((methodCall.openAngle.TokenIndex - methodCall.methodName.TokenIndex + 1)..(methodCall.closeAngle.TokenIndex - methodCall.methodName.TokenIndex))
					.Select(t => t.GetText())
					.StringJoin();
			}

			var signature = new MethodSignature(type, methodName, typeParameterText, argTypes, isStatic: false);

			if (_methodCache.TryGetValue(signature, out var cachedMethod))
				return await InvokeCachedMethod(cachedMethod.MethodInfo, target, args, cachedMethod.IsExtension);

			Type[]? typeParameters = null;


			if (methodCall.openAngle is not null)
			{
				var angle = typeParameterText;

				typeParameters = string.IsNullOrWhiteSpace(angle)
									? Type.EmptyTypes
									: GetGenericTypeArgument(angle);
			}

			var method = type.GetMethod(methodName, argTypes);

			if (method is not null)
				return await InvokeMethod(method, target, args, signature, false);

			var candidates = type.GetMethods().Where(m => m.Name == methodName && m.GetParameterLength() == argTypes.Length);

			foreach (var candidate in candidates)
				if (candidate.GetParameterTypes().SequenceEqual(argTypes, TypeEqualityComparer.Instance))
				{
					if(!candidate.IsGenericMethod)
						return await InvokeMethod(candidate, target, args, signature, false);

					var instanceLocations = GetTypeArgumentLocations(candidate);
					if (instanceLocations.Contains(TypeArgumentLocation.Nowhere))
						return await InvokeMethod(candidate.MakeGenericMethod(typeParameters ?? Type.EmptyTypes), target, args, signature, false);

					var candidateParameters = candidate.GetParameterTypes().ToArray();
					var castedInstanceParameters = argTypes
						.Zip(candidateParameters)
						.Select(t => t.Second.IsGenericParameter ? t.First : t.Second.IsGenericType ? t.First.GetInterface(t.Second.Name) ?? t.First : t.Second)
						.ToArray();

					var instanceGenericTypeArgs = instanceLocations.Select(l => l.Access(castedInstanceParameters)).ToArray();

					return await InvokeMethod(candidate.MakeGenericMethod(instanceGenericTypeArgs), target, args, signature, false);
				}

			var extensionDictionary = _customTypeProvider.GetAllExtensionMethods();

			var extensionMethods = extensionDictionary.SelectMany(kv => kv.Value).Where(m => m.Name == methodName && IsAssignableToGeneric(type, m.GetParameters()[0].ParameterType)).ToArray();

			var extensionParamTypes = new[] { type }.Concat(argTypes).ToArray();

			var extensionMethodCandidates = extensionMethods
				.Where(m => m
				.GetParameterTypes()
				.SequenceEqual(extensionParamTypes, TypeEqualityComparer.Instance));

			var extensionMethod = (typeParameters?.Length > 0 ? extensionMethodCandidates.Where(m => m.GetGenericArguments().Length == typeParameters.Length) : extensionMethodCandidates).FirstOrDefault();

			if (extensionMethod is null)
			{
				var methodSignature = $"{type.Name}.{methodName}({argTypes.Select(t => t.Name).StringJoin(", ")})";
				@throw = new NoSuchMethodException($"Could not find a matching method or an extension method '{methodSignature}'");
				return InvalidPostcall.Value;
			}

			var extensionParameters = new[] { target }.Concat(args).ToList();

			if (!extensionMethod.IsGenericMethod)
				return await InvokeMethod(extensionMethod, target, new[] { target }.Concat(args).ToList(), signature, true);

			if (typeParameters?.Length > 0)
				return await InvokeMethod(extensionMethod.MakeGenericMethod(typeParameters), target, extensionParameters, signature, true);

			var locations = GetTypeArgumentLocations(extensionMethod);

			if (locations.Contains(TypeArgumentLocation.Nowhere))
				return await InvokeMethod(extensionMethod.MakeGenericMethod(typeParameters ?? Type.EmptyTypes), target, extensionParameters, signature, true);

			var castedParameters = extensionParamTypes
				.Zip(extensionMethod.GetParameterTypes())
				.Select(t => t.Second.IsGenericParameter ? t.First : t.Second.IsGenericType ? t.First.GetInterface(t.Second.Name) ?? t.First : t.Second)
				.ToArray();

			var genericTypeArgs = locations.Select(l => l.Access(castedParameters)).ToArray();

			return await InvokeMethod(extensionMethod.MakeGenericMethod(genericTypeArgs), target, extensionParameters, signature, true);
		}
		catch (Exception ex)
		{
			@throw = ex;
			return InvalidPostcall.Value;
		}
	}

	private async Task<object?> StaticPostcall(Type? type, SBProcLangParser.MethodCallContext methodCall)
	{
		try
		{
			var methodName = methodCall.methodName.Text;

			if (type is null || methodName is null)
				return InvalidPostcall.Value;

			var argStr = methodCall.children
				.Take((methodCall.methodOpen.TokenIndex - methodCall.methodName.TokenIndex)..(methodCall.methodClose.TokenIndex - methodCall.methodName.TokenIndex))
				.OfType<SBProcLangParser.ExprContext>()
				.Select(c => c.GetText())
				.ToArray();

			var args = new List<object?>();

			foreach (var i in argStr)
				args.Add(await QueryRunner.EvaluateExpressionAsync(i));

			var argTypes = args.Select(a => a.GetTypeOrDefault()).ToArray();

			string? typeParameterText = null;

			if (methodCall.openAngle is not null)
			{
				typeParameterText = methodCall.children
					.Take((methodCall.openAngle.TokenIndex - methodCall.methodName.TokenIndex + 1)..(methodCall.closeAngle.TokenIndex - methodCall.methodName.TokenIndex))
					.Select(t => t.GetText())
					.StringJoin();
			}

			var signature = new MethodSignature(type, methodName, typeParameterText, argTypes, isStatic: true);

			if (_methodCache.TryGetValue(signature, out var cachedMethod))
				return await InvokeCachedMethod(cachedMethod.MethodInfo, null, args, cachedMethod.IsExtension);

			Type[]? typeParameters = null;

			if (methodCall.openAngle is not null)
			{
				var angle = typeParameterText;

				typeParameters = string.IsNullOrWhiteSpace(angle)
									? Type.EmptyTypes
									: GetGenericTypeArgument(angle);
			}

			var method = type.GetMethod(methodName, argTypes);

			if (method is null)
			{
				var methodSignature = $"{type.Name}.{methodName}({argTypes.Select(t => t.Name).StringJoin(", ")})";
				@throw = new NoSuchMethodException($"Could not find a matching static method '{methodSignature}'");
				return InvalidPostcall.Value;
			}

			if (!method.IsGenericMethod)
				return await InvokeMethod(method, null, args, signature, false);


			if (typeParameters?.Length > 0)
				return await InvokeMethod(method.MakeGenericMethod(typeParameters), null, args, signature, false);

			var locations = GetTypeArgumentLocations(method);

			if (locations.Contains(TypeArgumentLocation.Nowhere))
				return await InvokeMethod(method.MakeGenericMethod(typeParameters ?? Type.EmptyTypes), null, args, signature, false);


			var castedParameters = argTypes
				.Zip(method.GetParameterTypes())
				.Select(t => t.Second.IsGenericType ? t.First.GetInterface(t.Second.Name) ?? t.First : t.Second)
				.ToArray();

			var genericTypeArgs = locations.Select(l => l.Access(castedParameters)).ToArray();

			return await InvokeMethod(method.MakeGenericMethod(genericTypeArgs), null, args, signature, false);
		}
		catch (Exception ex)
		{
			@throw = ex;
			return InvalidPostcall.Value;
		}
	}

	private static async Task<object?> InvokeCachedMethod(MethodInfo method, object? obj, List<object?> args, bool isExtension)
	{
		var result = isExtension ? method.Invoke(null, new[] { obj }.Concat(args).ToArray()) : method.Invoke(obj, args.ToArray());

		if (method.ReturnType == typeof(void))
			result = string.Empty;

		else if (method.ReturnType == typeof(Task))
		{
			await (dynamic?)result;
			result = string.Empty;
		}

		else if (result.GetTypeOrDefault().IsSubclassOf(typeof(Task)))
			result = await (dynamic?)result;

		return result;
	}

	private static async Task<object?> InvokeMethod(MethodInfo method, object? obj, List<object?> args, MethodSignature signature, bool isExtension)
	{
		_methodCache.Add(signature, (method, isExtension));

		var result = method.Invoke(obj, args.ToArray());

		if (method.ReturnType == typeof(void))
			result = string.Empty;

		else if (method.ReturnType == typeof(Task))
		{
			await (dynamic?)result;
			result = string.Empty;
		}

		else if (result.GetTypeOrDefault().IsSubclassOf(typeof(Task)))
			result = await (dynamic?)result;

		return result;
	}

	private Type GetTypeByName(string name)
	{
		if (_typeCache.TryGetValue(name, out var cachedType))
			return cachedType;

		var result = GetTypeByNameCore(name);

		if (result != typeof(void))
			_typeCache.Add(name, result);

		return result;
	}

	private Type GetTypeByNameCore(string name)
	{
		if (name.EndsWith("[]"))
			return MakeArrayType(name);

		if (name.IndexOf('<') is var langle && langle != -1)
		{
			var typeBase = name[..langle];
			var rangle = Find.CloseBrace(name, '<', '>', langle);
			var typeArgs = GetGenericTypeArgument(name[(langle + 1)..rangle]);
			var type = _customTypeProvider.GetTypeByName($"{typeBase}`{typeArgs.Length}");
			return type.MakeGenericType(typeArgs);
		}

		return _customTypeProvider.GetTypeByName(name);
	}

	private Type MakeArrayType(string name)
	{
		var arrayDepth = 0;
		while (name.EndsWith("[]"))
		{
			name = name[..^2];
			arrayDepth++;
		}

		var type = GetTypeByName(name);

		while (arrayDepth > 0)
		{
			type = type.MakeArrayType();
			arrayDepth--;
		}

		return type;
	}

	private static bool IsAssignableToGeneric(Type? from, Type? to)
	{
		if(from is null || to is null) 
			return false;

		if (from.IsAssignableTo(to))
			return true;

		if (!to.IsGenericType)
			return false;

		var toGeneric = to.GetGenericTypeDefinition();

		if (from.IsGenericType && from.GetGenericTypeDefinition().IsAssignableTo(toGeneric))
			return true;

		if (from.GetInterface(toGeneric.FullName!) is not null)
			return true;

		if (from.IsGenericType && from.GetGenericTypeDefinition().GetInterface(toGeneric.FullName!) is not null)
			return true;

		return false;
	}

	private Type[] GetGenericTypeArgument(string argStr)
	{
		if (!argStr.Contains(','))
			return new[] { GetTypeByName(argStr) };

		return Split.By(',', argStr).Select(GetTypeByName).ToArray();
	}

	private static TypeArgumentLocation[] GetTypeArgumentLocations(MethodInfo method)
	{
		var location = Enumerable.Repeat(TypeArgumentLocation.Nowhere, method.GetGenericArguments().Length).ToArray();

		static TypeArgumentLocation GetLocationCore(Type typeArg, Type paramType, int paramIndex, int depth, List<int> indices)
		{
			if (paramType == typeArg)
				return new TypeArgumentLocation(paramIndex, depth, indices.ToArray());

			if (paramType.IsGenericType)
				foreach (var (innerArg, i) in paramType.GetGenericArguments().WithIndex())
				{
					indices.Add(i);

					var loc = GetLocationCore(typeArg, innerArg, paramIndex, depth + 1, indices);

					indices.RemoveAt(indices.Count - 1);

					if (loc != TypeArgumentLocation.Nowhere)
						return loc;
				}

			return TypeArgumentLocation.Nowhere;
		}

		foreach (var (typeArg, index) in method.GetGenericArguments().WithIndex())
			foreach (var (param, i) in method.GetParameters().WithIndex())
			{
				var loc = GetLocationCore(typeArg, param.ParameterType, i, 0, new());

				if (loc != TypeArgumentLocation.Nowhere)
					location[index] = loc;
			}

		return location;
	}

	private static object? MakeTuple(object?[] args)
	{
		if (args.Length == 0)
			return default(ValueTuple);

		var typeArgs = args.Select(a => a.GetTypeOrDefault()).ToArray();

		var valueTupleTypes = new[] 
		{
			typeof(ValueTuple<>), 
			typeof(ValueTuple<,>), 
			typeof(ValueTuple<,,>), 
			typeof(ValueTuple<,,,>), 
			typeof(ValueTuple<,,,,>),
			typeof(ValueTuple<,,,,,>),
			typeof(ValueTuple<,,,,,,>),
			typeof(ValueTuple<,,,,,,,>)
		};

		var count = typeArgs.Length;

		if (count < 8)
		{
			var shortTupleType = valueTupleTypes[count - 1].MakeGenericType(typeArgs);

			var shortCtor = shortTupleType.GetConstructor(typeArgs)
				?? throw new NoSuchConstructorException($"Could not find a matching constructor");

			return shortCtor.Invoke(args);
		}

		var restType = MakeTuple(args.Skip(7).ToArray()).GetTypeOrDefault();
		var longTypeArgs = typeArgs.Take(7).Concat(new[] { restType }).ToArray();
		var tupleType = valueTupleTypes[7].MakeGenericType(longTypeArgs);

		var ctor = tupleType.GetConstructor(longTypeArgs)
			?? throw new NoSuchConstructorException($"Could not find a matching constructor");

		return ctor.Invoke(count < 8 ? args : args[..7].Append(MakeTuple(args[7..])).ToArray());
	}
}

internal class NoSuchConstructorException : Exception
{
	public NoSuchConstructorException(string message) : base(message) { }
}

internal class NoSuchMethodException : Exception
{
	public NoSuchMethodException(string message) : base(message) { }
}

internal class TypeEqualityComparer : IEqualityComparer<Type>
{
	public static TypeEqualityComparer Instance { get; } = new();

	public bool Equals(Type? x, Type? y)
	{
		if (x is null || y is null)
			return false;

		if (x.IsAssignableFrom(y))
			return true;

		if (x.IsGenericParameter || y.IsGenericParameter)
			return true;

		if (Equals(x.GetElementType(), y.GetElementType()))
			return true;

		if (x.IsGenericType && y.IsGenericType && x.GetGenericTypeDefinition().IsAssignableFrom(y.GetGenericTypeDefinition()))
			return true;

		if (x.IsGenericType && y.GetInterface(x.GetGenericTypeDefinition().FullName ?? string.Empty) is not null)
			return true;

		return false;
	}

	public int GetHashCode([DisallowNull] Type obj)
	{
		throw new NotImplementedException();
	}
}

internal readonly record struct TypeArgumentLocation(int Index, int Depth, int[] TypeIndex)
{
	public static readonly TypeArgumentLocation Nowhere = new(-1, -1, null!);

	public Type Access(Type[] types)
	{
		var type = types[Index];

		if (Depth < 1)
			return type;

		var result = type;
		for (var i = 0; i < Depth; i++)
			result = result.GetGenericArgumentsOrElementTypes()[TypeIndex[i]];

		return result;
	}
}



internal static partial class PostcallRegex
{
	[GeneratedRegex(@"^[A-Z_a-z][0-9A-Z_a-z<>\[\],]*$")]
	internal static partial Regex GenericTypeName();
}

internal sealed class InvalidPostcall
{
	internal static readonly InvalidPostcall Value = new();

	public override string ToString() => "Invalid postcall";
}

internal sealed class ConstructorSignature : IEquatable<ConstructorSignature>
{
	private readonly string _value;
	internal ConstructorSignature(Type type, Type[] parameters)
		=> _value = $"{type.FullName}({parameters.Select(p => p.FullName).StringJoin(", ")})";
	public bool Equals(ConstructorSignature? other) => _value == other?._value;
	public override bool Equals(object? obj) => Equals(obj as ConstructorSignature);
	public override int GetHashCode() => _value.GetHashCode();
	public override string ToString() => _value;
}

internal sealed class MethodSignature : IEquatable<MethodSignature>
{
	private readonly string _value;
	internal MethodSignature(Type target, string methodName, string? typeParameterText, Type[] parameters, bool isStatic = false) 
		=> _value = $"{(isStatic ? "static" : "instance")} {target.FullName}.{methodName}<{typeParameterText}>({parameters.Select(p => p.FullName).StringJoin(", ")})";
	public bool Equals(MethodSignature? other) => _value == other?._value;
	public override bool Equals(object? obj) => Equals(obj as MethodSignature);
	public override int GetHashCode() => _value.GetHashCode();
	public override string ToString() => _value;
}