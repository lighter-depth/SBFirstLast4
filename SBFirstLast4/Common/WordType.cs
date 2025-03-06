using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4;

[DynamicLinqType]
public enum WordType
{
	Empty, Normal, Animal, Plant, Place, Emote, Art, Food, Violence, Health, Body, Mech, Science, Time, Person, Work, Cloth, Society, Play, Bug, Math, Insult, Religion, Sports, Weather, Tale
}

[DynamicLinqType]
public static class WordTypeEx
{
	public static WordType StringToType(this string symbol) => symbol switch
	{
		"ノーマル" => WordType.Normal,
		"動物" => WordType.Animal,
		"植物" => WordType.Plant,
		"地名" => WordType.Place,
		"感情" => WordType.Emote,
		"芸術" => WordType.Art,
		"食べ物" => WordType.Food,
		"暴力" => WordType.Violence,
		"医療" => WordType.Health,
		"人体" => WordType.Body,
		"機械" => WordType.Mech,
		"理科" => WordType.Science,
		"時間" => WordType.Time,
		"人物" => WordType.Person,
		"工作" => WordType.Work,
		"服飾" => WordType.Cloth,
		"社会" => WordType.Society,
		"遊び" => WordType.Play,
		"虫" => WordType.Bug,
		"数学" => WordType.Math,
		"暴言" => WordType.Insult,
		"宗教" => WordType.Religion,
		"スポーツ" => WordType.Sports,
		"天気" => WordType.Weather,
		"物語" => WordType.Tale,
		_ => WordType.Empty
	};

	public static WordType SpanToType(this ReadOnlySpan<char> symbol, ListLoader.WordTypeFormat format) => format switch
	{
		ListLoader.WordTypeFormat.FullName => SpanToType(symbol),
		ListLoader.WordTypeFormat.TypeCode => CharToType(symbol[0]),
		ListLoader.WordTypeFormat.Abbreviated => symbol[0] switch
		{
			'ノ' => WordType.Normal,
			'動' => WordType.Animal,
			'植' => WordType.Plant,
			'地' => WordType.Place,
			'感' => WordType.Emote,
			'芸' => WordType.Art,
			'食' => WordType.Food,
			'暴' => WordType.Violence,
			'医' => WordType.Health,
			'体' => WordType.Body,
			'機' => WordType.Mech,
			'理' => WordType.Science,
			'時' => WordType.Time,
			'人' => WordType.Person,
			'工' => WordType.Work,
			'服' => WordType.Cloth,
			'社' => WordType.Society,
			'遊' => WordType.Play,
			'虫' => WordType.Bug,
			'数' => WordType.Math,
			'言' => WordType.Insult,
			'宗' => WordType.Religion,
			'ス' => WordType.Sports,
			'天' => WordType.Weather,
			'物' => WordType.Tale,
			_ => WordType.Empty
		},
		_ => SpanToType(symbol)
	};

	public static WordType SpanToType(this ReadOnlySpan<char> symbol) => symbol switch
	{
		"ノーマル" => WordType.Normal,
		"動物" => WordType.Animal,
		"植物" => WordType.Plant,
		"地名" => WordType.Place,
		"感情" => WordType.Emote,
		"芸術" => WordType.Art,
		"食べ物" => WordType.Food,
		"暴力" => WordType.Violence,
		"医療" => WordType.Health,
		"人体" => WordType.Body,
		"機械" => WordType.Mech,
		"理科" => WordType.Science,
		"時間" => WordType.Time,
		"人物" => WordType.Person,
		"工作" => WordType.Work,
		"服飾" => WordType.Cloth,
		"社会" => WordType.Society,
		"遊び" => WordType.Play,
		"虫" => WordType.Bug,
		"数学" => WordType.Math,
		"暴言" => WordType.Insult,
		"宗教" => WordType.Religion,
		"スポーツ" => WordType.Sports,
		"天気" => WordType.Weather,
		"物語" => WordType.Tale,
		_ => WordType.Empty
	};

	public static string TypeToString(this WordType type) => type switch
	{
		WordType.Empty => string.Empty,
		WordType.Normal => "ノーマル",
		WordType.Animal => "動物",
		WordType.Plant => "植物",
		WordType.Place => "地名",
		WordType.Emote => "感情",
		WordType.Art => "芸術",
		WordType.Food => "食べ物",
		WordType.Violence => "暴力",
		WordType.Health => "医療",
		WordType.Body => "人体",
		WordType.Mech => "機械",
		WordType.Science => "理科",
		WordType.Time => "時間",
		WordType.Person => "人物",
		WordType.Work => "工作",
		WordType.Cloth => "服飾",
		WordType.Society => "社会",
		WordType.Play => "遊び",
		WordType.Bug => "虫",
		WordType.Math => "数学",
		WordType.Insult => "暴言",
		WordType.Religion => "宗教",
		WordType.Sports => "スポーツ",
		WordType.Weather => "天気",
		WordType.Tale => "物語",
		_ => "天で話にならねぇよ..."
	};

	public static string AbilityToString(this WordType type) => type switch
	{
		WordType.Empty => string.Empty,
		WordType.Normal => "デバッガー",
		WordType.Animal => "はんしょく",
		WordType.Plant => "やどりぎ",
		WordType.Place => "グローバル",
		WordType.Emote => "じょうねつ",
		WordType.Art => "ロックンロール",
		WordType.Food => "いかすい",
		WordType.Violence => "むきむき",
		WordType.Health => "医食",
		WordType.Body => "からて",
		WordType.Mech => "かちこち",
		WordType.Science => "じっけん",
		WordType.Time => "さきのばし",
		WordType.Person => "きょじん",
		WordType.Work => "ぶそう",
		WordType.Cloth => "かさねぎ",
		WordType.Society => "ほけん",
		WordType.Play => "かくめい",
		WordType.Bug => "どくばり",
		WordType.Math => "けいさん",
		WordType.Insult => "ずぼし",
		WordType.Religion => "しんこうしん",
		WordType.Sports => "トレーニング",
		WordType.Weather => "たいふういっか",
		WordType.Tale => "俺文字",
		_ => "天で話にならねぇよ..."
	};
	public static WordType CharToType(this char symbol) => symbol switch
	{
		'N' or 'n' => WordType.Normal,
		'A' or 'a' => WordType.Animal,
		'Y' or 'y' => WordType.Plant,
		'G' or 'g' => WordType.Place,
		'E' or 'e' => WordType.Emote,
		'C' or 'c' => WordType.Art,
		'F' or 'f' => WordType.Food,
		'V' or 'v' => WordType.Violence,
		'H' or 'h' => WordType.Health,
		'B' or 'b' => WordType.Body,
		'M' or 'm' => WordType.Mech,
		'Q' or 'q' => WordType.Science,
		'T' or 't' => WordType.Time,
		'P' or 'p' => WordType.Person,
		'K' or 'k' => WordType.Work,
		'L' or 'l' => WordType.Cloth,
		'S' or 's' => WordType.Society,
		'J' or 'j' => WordType.Play,
		'D' or 'd' => WordType.Bug,
		'X' or 'x' => WordType.Math,
		'Z' or 'z' => WordType.Insult,
		'R' or 'r' => WordType.Religion,
		'U' or 'u' => WordType.Sports,
		'W' or 'w' => WordType.Weather,
		'O' or 'o' => WordType.Tale,
		_ => WordType.Empty // I is not used
	};

	public static char TypeToChar(this WordType type) => type switch
	{
		WordType.Animal => 'A',
		WordType.Body => 'B',
		WordType.Art => 'C',
		WordType.Bug => 'D',
		WordType.Emote => 'E',
		WordType.Food => 'F',
		WordType.Place => 'G',
		WordType.Health => 'H',
		WordType.Empty => 'I',
		WordType.Play => 'J',
		WordType.Work => 'K',
		WordType.Cloth => 'L',
		WordType.Mech => 'M',
		WordType.Normal => 'N',
		WordType.Tale => 'O',
		WordType.Person => 'P',
		WordType.Science => 'Q',
		WordType.Religion => 'R',
		WordType.Society => 'S',
		WordType.Time => 'T',
		WordType.Sports => 'U',
		WordType.Violence => 'V',
		WordType.Weather => 'W',
		WordType.Math => 'X',
		WordType.Plant => 'Y',
		WordType.Insult => 'Z',
		_ => 'I'
	};

	public static string TypeToImg(this WordType type) => $"images/{Enum.GetName(type)?.ToLower() + ".gif" ?? string.Empty}";

	public static string TypeToAudio(this WordType type) => Enum.GetName(type)?.ToLower() ?? string.Empty;
}