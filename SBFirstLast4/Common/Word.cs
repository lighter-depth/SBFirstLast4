using SBFirstLast4.Simulator;
using System.Diagnostics;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Runtime.CompilerServices;
using MultiWord = SBFirstLast4.Dynamic.Extensions.MultiWord;

namespace SBFirstLast4;

[DynamicLinqType]
[DebuggerDisplay($"{nameof(Name)} ({nameof(Type1)}/{nameof(Type2)})")]
public readonly record struct Word(string Name, WordType Type1, WordType Type2) : IComparable<Word>
{
	public char Start => Name.At(0);

	public char End => Name.GetLastChar();

	public int Length => Name.Length;

	public List<WordType> Types => IsEmpty ? [] : IsSingleType ? [Type1] : [Type1, Type2];

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

	public Word(string? name, string? type1, string? type2)
		: this(name ?? string.Empty, type1?.StringToType() ?? WordType.Empty, type2?.StringToType() ?? WordType.Empty) { }

	public Word(MultiWord word)
		: this(word.Name, word.Types.At(0), word.Types.At(1)) { }

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public double CalcEffectiveDmg(Word receiver)
		=> CalcEffectiveDmg(Type1, receiver.Type1)
		   * CalcEffectiveDmg(Type1, receiver.Type2)
		   * CalcEffectiveDmg(Type2, receiver.Type1)
		   * CalcEffectiveDmg(Type2, receiver.Type2);

	public double CalcEffectiveDmg(MultiWord receiver)
		=> new MultiWord(this).CalcEffectiveDmg(receiver);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			_ => 1
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

		public const int Suitable = 0,
						 InvalidEnd = -1,
						 BadStart = 1;
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
		typeIndex =
		[
			WordType.Violence, WordType.Food, WordType.Place, WordType.Society, WordType.Animal,
			WordType.Emote, WordType.Plant, WordType.Science, WordType.Play, WordType.Person,
			WordType.Cloth, WordType.Work, WordType.Art, WordType.Body, WordType.Time,
			WordType.Mech, WordType.Health, WordType.Tale, WordType.Insult, WordType.Math,
			WordType.Weather, WordType.Bug, WordType.Religion, WordType.Sports, WordType.Normal,
			WordType.Empty
		];
	}
	public static explicit operator Word(string name) => new(name, WordType.Empty, WordType.Empty);

	public static explicit operator Word(MultiWord word) => new(word);

	public static Word FromString(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Default;

		var list = Words.GetSplitList(name[0]);

		var resultWord = Words.GetSplitList(name[0]).Find(x => x.Name == name);

		if (resultWord != default)
			return resultWord;

		return (Word)name;
	}

	public static Word FromVerbatim(string? name, string? type1Str, string? type2Str)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Default;

		var type1 = type1Str?.StringToType() ?? WordType.Empty;
		var type2 = type2Str?.StringToType() ?? WordType.Empty;

		return new(name, type1, type2);
	}

	internal static Word FromType(WordType type) => Default with { Type1 = type };
}