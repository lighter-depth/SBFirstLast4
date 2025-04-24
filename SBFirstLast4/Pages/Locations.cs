using SharpCompress.Compressors.Xz;

namespace SBFirstLast4.Pages;

public static class Locations
{
	public const string
		Index = "",
		Auth = "auth",
		Top = "top",
		Setting = "settings",
		About = "about";

	public static class Searching
	{
		public const string
			TL = "home",
			TD = "typed",
			Safe = "safe",
			Group = "treesearch",
			Fast = "fastsearch",
			Result = "result",
			GroupResult = "treesearch-result",
			Download = "download-config";
	}

	public static class Tools
	{
		public const string
			TypeChecker = "typecheck",
			BestDamage = "bestdamage",
			CalcDamage = "calcdamage",
			ManualQuery = "manualquery",
			VolatileQuery = "volatilequery",
			SoundRoom = "sound-room";
	}

	public static class Simulator
	{
		public const string
			Top = "simulator",
			Main = "simulator-body",
			Skill = "simulator-skill";
	}

	public static class RevSimulator
	{
		public const string
			Main = "rev-simulator",
			Result = "rev-simulator-result";
	}

	public static class LD
	{
		public const string
			Load = "ldload",
			Edit = "ldedit",
			Loading = "ldloading";
	}

	public static class Minigame
	{
		public const string
			Menu = "minigame",
			Wordle = "wordle",
			TornGame = "torngame";
	}

	public static class Others
	{
		public const string
			Debug = "debug",
			Online = "online-menu";
	}
}
