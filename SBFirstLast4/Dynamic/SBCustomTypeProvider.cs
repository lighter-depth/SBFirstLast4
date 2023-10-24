using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic;

public class SBCustomTypeProvider : DefaultDynamicLinqCustomTypeProvider, IDynamicLinkCustomTypeProvider
{

	public SBCustomTypeProvider(bool cacheCustomTypes = true) : base(cacheCustomTypes) { }


	public override HashSet<Type> GetCustomTypes()
	{
		var types = base.GetCustomTypes();
		types = types.Concat(typeof(System.Text.RegularExpressions.Regex).Assembly.GetTypes()).ToHashSet();
		return types;
	}
}
