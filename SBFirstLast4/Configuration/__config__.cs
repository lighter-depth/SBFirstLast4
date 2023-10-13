#pragma warning disable IDE1006

namespace SBFirstLast4.Configuration;

internal static class __config__
{
	private static int _convention;

	internal static string FromStoredValue()
	{
		if (_convention == 0)
		{
			_convention++;
			return Convert.ToBase64String(__provider__.ConventionStorage(_convention));
		}
		_convention = 0;
		return Convert.ToBase64String(__provider__.ConventionStorage(_convention));

	}
}
