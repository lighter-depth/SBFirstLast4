using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace SBFirstLast4;

[DynamicLinqType]
public readonly record struct AttackInfo(Word Word, WordType Ability, int MaxDmg, int MinDmg) : IComparable<AttackInfo>
{
	public static AttackInfo Create(Word word, WordType ability, int max, int min) => new(word, ability, max, min);
	public override string ToString()
	{
		var ability = Ability == WordType.Empty ? string.Empty : " [" + Ability.AbilityToString() + "] ";
		return Word + ability + $"{{{MinDmg}-{MaxDmg}}}";
	}
	public int CompareTo(AttackInfo other) => -MaxDmg.CompareTo(other.MaxDmg);
}
