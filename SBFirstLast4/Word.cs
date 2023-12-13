using SBFirstLast4.Simulator;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4;

[DynamicLinqType]
public readonly record struct Word(string Name, WordType Type1, WordType Type2) : IComparable<Word>
{
	public char Start => Name.At(0);
	public char End => Name.GetLastChar();
	public int Length => Name.Length;

	public List<WordType> Types
	{
		get
		{
			if (IsEmpty) return new();
			if (IsSingleType) return new() { Type1 };
			return new() { Type1, Type2 };
		}
	}
	public bool Contains(WordType type) => Type1 == type || Type2 == type;
	public bool Contains(WordType type1, WordType type2) => Type1 == type1 || Type2 == type1 || Type1 == type2 || Type2 == type2;

	public bool Contains(IEnumerable<WordType> types) => this is var word && types.Any(word.Contains);

	public bool IsEmpty => Type1 == WordType.Empty;
	public bool IsSingleType => Type1 != WordType.Empty && Type2 == WordType.Empty;
	public bool IsDoubleType => Type1 != WordType.Empty && Type2 != WordType.Empty;

	public bool IsHeal => Contains(WordType.Food) || Contains(WordType.Health);

	public bool IsViolence => !IsHeal && Contains(WordType.Violence);
	public bool IsCritable => Contains(WordType.Body) || Contains(WordType.Insult);
	public bool IsDefault => this == Default;
	public static Word Default => _default;
	private static readonly Word _default = new(string.Empty, WordType.Empty, WordType.Empty);

	private static readonly int[,] effList;
	private static readonly WordType[] typeIndex;
	public override string ToString()
	{
		var type1 = Type1.TypeToString();
		var type2 = Type2.TypeToString();
		return IsEmpty ? Name
			 : IsSingleType ? $"{Name} ({type1})"
			 : $"{Name} ({type1}/{type2})";
	}
	public string ToFormat(ListFormat formatType, WordType omitType = WordType.Empty)
	{
		var word = this with { };
		if (omitType != WordType.Empty && formatType != ListFormat.SimulatorCsv)
		{
			var otherType = Type1 == omitType ? Type2 : Type1;
			word = word with { Type1 = otherType, Type2 = WordType.Empty };
		}
		var type1 = word.Type1 != WordType.Empty ? word.Type1.TypeToString() : string.Empty;
		var type2 = word.Type2 != WordType.Empty ? word.Type2.TypeToString() : string.Empty;
		if (formatType == ListFormat.SlashBracket) return word.ToString();
		return word.Name + toSpace(word.Type1) + type1 + toSpace(word.Type2) + type2;
		static string toSpace(WordType type) => type != WordType.Empty ? " " : string.Empty;
	}

	public int CompareTo(Word other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
	public double CalcEffectiveDmg(Word other)
		=> CalcEffectiveDmg(Type1, other.Type1)
		   * CalcEffectiveDmg(Type1, other.Type2)
		   * CalcEffectiveDmg(Type2, other.Type1)
		   * CalcEffectiveDmg(Type2, other.Type2);

	public static double CalcEffectiveDmg(WordType t1, WordType t2)
	{
		if (t1 == WordType.Empty || t2 == WordType.Empty) return 1;
		var t1Index = typeIndex.IndexOf(t1);
		var t2Index = typeIndex.IndexOf(t2);
		return effList[t1Index, t2Index] switch
		{
			0 => 1,
			1 => 2,
			2 => 0.5,
			3 => 0,
			_ => throw new ArgumentOutOfRangeException($"パラメーター{effList[t1Index, t2Index]} は無効です。")
		};
	}

	public bool IsBuf(Ability ability) => ability is ISingleTypedBufAbility buf && Contains(buf.BufType);
	public bool IsSeed(Ability ability) => ability is ISeedable seed && Contains(seed.SeedType);
	public SuitableIndicator IsSuitable(Word prev)
	{
		if (End == 'ん')
			return SuitableIndicator.InvalidEnd;
		if (string.IsNullOrWhiteSpace(prev.Name))
			return SuitableIndicator.Suitable;
		if (Start.IsWild() || prev.End.IsWild())
			return SuitableIndicator.Suitable;
		if (Start != prev.End)
			return SuitableIndicator.BadStart;
		return SuitableIndicator.Suitable;
	}

	public readonly struct SuitableIndicator
	{
		private int Value { get; init; }

		public static implicit operator SuitableIndicator(int @int) => new() { Value = @int };
		public static implicit operator int(SuitableIndicator s) => s.Value;
		public static implicit operator bool(SuitableIndicator s) => s.Value == 0;
		public static implicit operator SuitableIndicator(bool @bool) => new() { Value = @bool ? 0 : 1 };

		public const int Suitable = 0;
		public const int InvalidEnd = -1;
		public const int BadStart = 1;
	}

	public string Serialize() => $"/w/{Name}++{Type1.TypeToChar()}++{Type2.TypeToChar()}/w/";

	public static Word Deserialize(string? str)
	{
		if (str is null)
			return Default;

		str = str.Trim();
		if (!str.StartsWith("/w/") || !str.EndsWith("/w/") || str.Length < 4)
			return Default;

		var wordData = str[3..^3].Split("++");

		var name = wordData.At(0) ?? string.Empty;
		var type1 = wordData.At(1)?.At(0).CharToType() ?? default;
		var type2 = wordData.At(2)?.At(0).CharToType() ?? default;

		return new(name, type1, type2);
	}

	static Word()
	{
		// 0: Normal, 1: Effective, 2: Non-Effective, 3: No Damage
		effList = new[,]
		{
			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 3, 3, 2, 1, 1, 1 }, // Violence
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, // Food
            { 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Place
            { 1, 0, 0, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0 }, // Society
            { 2, 1, 0, 0, 2, 0, 1, 2, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 }, // Animal
            { 2, 0, 0, 1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 3, 0, 0, 2, 2, 0, 0, 2, 0, 0 }, // Emotion
            { 0, 1, 1, 0, 2, 0, 2, 0, 2, 0, 2, 2, 0, 1, 1, 2, 0, 0, 0, 0, 0, 2, 0, 0, 0 }, // Plant
            { 0, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2, 0, 0 }, // Science
            { 2, 2, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 }, // Playing
            { 2, 0, 0, 2, 2, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0 }, // Person
            { 2, 0, 0, 0, 0, 0, 1, 0, 2, 0, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Clothing
            { 2, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Work
            { 2, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, // Art
            { 2, 1, 0, 0, 2, 0, 2, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Body
            { 0, 1, 0, 0, 0, 0, 2, 0, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Time
            { 2, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Machine
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, // Health
            { 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0 }, // Tale
            { 2, 2, 0, 1, 2, 1, 2, 0, 1, 1, 1, 0, 1, 1, 0, 2, 0, 0, 1, 1, 3, 2, 2, 1, 0 }, // Insult
            { 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0 }, // Math
            { 1, 1, 1, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 2, 2, 0, 1, 0 }, // Weather
            { 1, 1, 0, 0, 1, 0, 1, 2, 0, 0, 2, 0, 0, 1, 0, 2, 1, 0, 1, 0, 0, 2, 0, 0, 0 }, // Bug
            { 2, 0, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 2, 0, 0 }, // Religion
            { 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 2, 0 }, // Sports
            { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }  // Normal  
        };
		typeIndex = new[]
		{
			WordType.Violence, WordType.Food, WordType.Place, WordType.Society, WordType.Animal,
			WordType.Emote, WordType.Plant, WordType.Science, WordType.Play, WordType.Person,
			WordType.Cloth, WordType.Work, WordType.Art, WordType.Body, WordType.Time,
			WordType.Mech, WordType.Health, WordType.Tale, WordType.Insult, WordType.Math,
			WordType.Weather, WordType.Bug, WordType.Religion, WordType.Sports, WordType.Normal,
			WordType.Empty
		};
	}
	public static explicit operator Word(string name) => new(name, WordType.Empty, WordType.Empty);

	public static Word FromString(string? name)
	{
		if (string.IsNullOrWhiteSpace(name)) return Default;
		var resultWord = SBDictionary.GetSplitList(name[0]).Find(x => x.Name == name);
		if (resultWord != default) return resultWord;
		return (Word)name;
	}

	internal static Word FromType(WordType type) => Default with { Type1 = type };
}

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