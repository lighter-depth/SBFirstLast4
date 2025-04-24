namespace SBFirstLast4.Logging;

internal static class StatusTemplate
{
	internal static StatusTemplate<T> Create<T>(string type, T order) => new(type, order);
}

public sealed class StatusTemplate<T>(string type, T order)
{
	public string Type { get; } = type;

	public T Order { get; } = order;

	public string Gen => Versioning.Generation.Name;

	public Version Version => Versioning.VersionHistory.Latest;

	public UserInfoTemplate UserInfo => new(AppSettings.UserName, AppSettings.Guid, AppSettings.Hash, AppSettings.V4, AppSettings.V6);

	public DateTime Date => DateTime.Now;

	public sealed record UserInfoTemplate(string UserName, string Guid, string Hash, string V4, string V6);
}

public static class StatusTypes
{
	public const string
		Register = "REGISTER",
		Login = "LOGIN",
		IllegalLogin = "ILLEGAL_LOGIN",
		SearchTL = "SEARCH_TL",
		SearchTD = "SEARCH_TD",
		SearchSafe = "SEARCH_SAFE",
		GroupSearchOpen = "GROUP_SEARCH_OPEN",
		SearchGroup = "SEARCH_GROUP",
		Download = "DOWNLOAD",
		TypeCheck = "TYPE_CHECK",
		BestDmg = "BEST_DMG",
		CalcDmg = "CALC_DMG",
		Simulator = "SIMULATOR",
		RevSimulatorOpen = "REV_SIMULATOR_OPEN",
		RevSimulator = "REV_SIMULATOR",
		FastSearch = "FAST_SEARCH",
		Query = "QUERY",
		QueryDocument = "QUERY_DOCUMENT",
		QueryMacro = "QUERY_MACRO",
		Monitor = "MONITOR",
		VolatileQueryOpen = "VOLATILE_QUERY_OPEN",
		LDLoad = "LD_LOAD",
		About = "ABOUT",
		SoundRoom = "SOUND_ROOM",
		UserName = "USERNAME",
		ToggleBeta = "TOGGLE_BETA",
		Logout = "LOGOUT";
}
