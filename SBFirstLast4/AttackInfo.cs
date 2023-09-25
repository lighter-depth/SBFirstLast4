namespace SBFirstLast4;

public readonly record struct AttackInfo(Word Word, WordType Ability, int MaxDmg, int MinDmg) : IComparable<AttackInfo>
{
	public override string ToString()
	{
		var ability = Ability == WordType.Empty ? string.Empty : " [" + Ability.AbilityToString() + "] ";
		return Word.ToString() + ability + $"{{{MinDmg}-{MaxDmg}}}";
	}
	public int CompareTo(AttackInfo other) => -MaxDmg.CompareTo(other.MaxDmg);
}
