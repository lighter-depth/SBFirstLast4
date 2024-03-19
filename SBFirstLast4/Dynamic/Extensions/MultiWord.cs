using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4.Dynamic.Extensions;

[DynamicLinqType]
public readonly record struct MultiWord(string Name, IReadOnlyList<WordType> Types) : IComparable<MultiWord>
{
	public char Start => Name.At(0);
	public char End => Name.GetLastChar();
	public int Length => Name.Length;
	public bool Contains(WordType type) => Types.Contains(type);
	public bool Contains(IEnumerable<WordType> types) => types.Intersect(Types).Any();
	public bool IsEmpty => Types.Count == 0;
	public bool IsSingleType => Types.Count == 1;
	public bool IsDoubleType => Types.Count == 2;

	public bool IsHeal => Contains(WordType.Food) || Contains(WordType.Health);

	public bool IsViolence => !IsHeal && Contains(WordType.Violence);
	public bool IsCritable => Contains(WordType.Body) || Contains(WordType.Insult);
	public bool IsDefault => this == Default;
	public static MultiWord Default => _default;
	private static readonly MultiWord _default = new(string.Empty, new List<WordType>());

	public MultiWord(string name, params WordType[] types) : this(name, types.ToList()) { }

	public MultiWord(Word word) : this(word.Name, word.Types) { }

	public override string ToString()
		=> IsEmpty ? Name : $"{Name} ({Types.Select(t => t.TypeToString()).StringJoin("/")})";

	public int CompareTo(MultiWord other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

	public double CalcEffectiveDmg(MultiWord other)
	{
		var result = 1d;

		for (var i = 0; i < Types.Count; i++)
			for (var j = 0; j < other.Types.Count; j++)
				result *= Word.CalcEffectiveDmg(Types[i], other.Types[j]);

		return result;
	}

	public double CalcEffectiveDmg(Word other) => CalcEffectiveDmg(new MultiWord(other));

	public Word.SuitableIndicator IsSuitable(MultiWord prev)
	{
		if (End == 'ん')
			return Word.SuitableIndicator.InvalidEnd;
		if (string.IsNullOrWhiteSpace(prev.Name))
			return Word.SuitableIndicator.Suitable;
		if (Start.IsWild() || prev.End.IsWild())
			return Word.SuitableIndicator.Suitable;
		if (Start != prev.End)
			return Word.SuitableIndicator.BadStart;
		return Word.SuitableIndicator.Suitable;
	}

	public static explicit operator MultiWord(string name) => new(name, new List<WordType>());

	public static explicit operator MultiWord(Word word) => new(word);

	public static MultiWord FromVerbatim(string? name, params string?[]? types)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Default;

		return new(name, types?.Select(s => s?.StringToType() ?? default).ToList() ?? new List<WordType>());
	}
}
