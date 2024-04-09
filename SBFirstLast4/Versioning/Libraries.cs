namespace SBFirstLast4.Versioning;

internal static class Libraries
{
	internal static readonly Library[] MIT =
	[
		("Antlr4BuildTasks", new(12, 8, 0), "https://github.com/kaby76/Antlr4BuildTasks"),
		("BlazorDownloadFile", new(2, 4, 0, 2), "https://github.com/arivera12/BlazorDownloadFile"),
		("Blazored.LocalStorage", new(4, 4, 0), "https://github.com/Blazored/LocalStorage"),
		("SpawnDev.BlazorJS", new(2, 2, 57), "https://github.com/LostBeard/SpawnDev.BlazorJS"),
		("Dynamic LINQ", new(1, 3, 5), "https://dynamic-linq.net/")
	];

	internal static readonly Library[] Apache = 
	[
		("mecab-ipadic-neologd", new(0, 0, 7), "https://github.com/neologd/mecab-ipadic-neologd")
	];


	internal static readonly Library[] BSD3 =
	[
		("ANTLR v4", new(4, 13, 1), "https://github.com/antlr/antlr4")
	];
}

internal readonly record struct Library(string Name, Version Version, string Url) 
{
	public static implicit operator Library((string, Version, string) t) => new(t.Item1, t.Item2, t.Item3);
}
