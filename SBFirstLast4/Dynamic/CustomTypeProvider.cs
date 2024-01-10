using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic;

public class CustomTypeProvider : DefaultDynamicLinqCustomTypeProvider, IDynamicLinkCustomTypeProvider
{

	public CustomTypeProvider(bool cacheCustomTypes = true) : base(cacheCustomTypes) { }


	public override HashSet<Type> GetCustomTypes()
	{
		var types = base.GetCustomTypes();
		types = types
				.Concat(typeof(System.Text.RegularExpressions.Regex).Assembly.GetTypes())
				.Concat(Record.Types)
				.ToHashSet();
        
		return types;
	}
}
