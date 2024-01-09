namespace SBFirstLast4.Simulator;

public class Player
{
	#region properties

	public string Name { get; internal set; } = "Player1";
	public int HP
	{
		get => _hp;
		set => _hp = Math.Max(0, Math.Min(value, MaxHP));
	}
	private int _hp = MaxHP;
	public const int MaxHP = 60;

	public Ability Ability { get; private set; }


	public double ATK => BufValues[ATKIndex];

	public int ATKIndex { get; private set; } = 6;

	public double DEF => BufValues[DEFIndex];

	public int DEFIndex { get; private set; } = 6;
	private static readonly double[] BufValues = { 0.25, 0.28571429, 0.33333333, 0.4, 0.5, 0.66666666, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0 };

	public Word CurrentWord { get; internal set; } = Word.Default;

	public PlayerState State { get; private set; }

	public int FoodCountRemain { get; private set; } = MaxFoodCount;

	public int CureCountRemain { get; private set; } = MaxCureCount;

	public int PoisonDmg { get; private set; }

	public int SeedTurnRemain { get; private set; }
	public const int SeedDmg = 5;
	public const int MaxSeedTurn = 4;
	public int SkillChangeRemain { get; private set; } = MaxAbilChange;
	public Proceeds Proceeds { get; internal set; } = Proceeds.Random;

	public List<BredString> BrdBuf { get; set; } = new();

	public bool IsPoisoned => State.HasFlag(PlayerState.Poison);

	public bool IsSeeded => State.HasFlag(PlayerState.Seed);
	#endregion

	public Player(Ability ability) => Ability = ability;

	public const int MaxAbilChange = 2;

	public const int MaxCureCount = 5;

	public const int MaxFoodCount = 6;

	public const double CritDmg = 1.5;

	public const int InsBufQty = 3;

	#region methods


	public bool TryChangeAbility(Ability ability)
	{
		if (SkillChangeRemain > 0)
		{
			Ability = ability;
			SkillChangeRemain--;
			return true;
		}
		return false;
	}

	public void ChangeSkill(Ability ability)
	{
		Ability = ability;
		SkillChangeRemain--;
	}
	public bool TryChangeATK(int arg, Word word)
	{
		var resultIndex = ATKIndex + arg;
		var maxIndex = BufValues.Length - 1;
		if ((ATKIndex == maxIndex && resultIndex > maxIndex)
			|| (ATKIndex == 0 && resultIndex < 0))
			return false;
		ATKIndex = Math.Max(0, Math.Min(resultIndex, maxIndex));
		CurrentWord = word;
		return true;
	}

	public bool TryChangeDEF(int arg, Word word)
	{
		var resultIndex = DEFIndex + arg;
		var maxIndex = BufValues.Length - 1;
		if ((DEFIndex == maxIndex && resultIndex > maxIndex)
			|| (DEFIndex == 0 && resultIndex < 0))
			return false;
		DEFIndex = Math.Max(0, Math.Min(resultIndex, maxIndex));
		CurrentWord = word;
		return true;
	}

	public void Poison()
	{
		State |= PlayerState.Poison;
		PoisonDmg = 0;
	}

	public void DePoison()
	{
		State &= ~PlayerState.Poison;
		PoisonDmg = 0;
	}

	public void Seed()
	{
		State |= PlayerState.Seed;
		SeedTurnRemain = MaxSeedTurn;
	}

	public void DeSeed()
	{
		State &= ~PlayerState.Seed;
		SeedTurnRemain = 0;
	}

	public void TakePoisonDmg()
	{
		PoisonDmg += (int)(MaxHP * 0.0625);
		HP -= PoisonDmg;
	}

	public void TakeSeedDmg(Player from)
	{
		HP -= SeedDmg;
		from.HP += SeedDmg;
		SeedTurnRemain--;
		if (SeedTurnRemain == 0) State &= ~PlayerState.Seed;
	}
	public void Heal(bool isCure)
	{
		if (isCure)
		{
			HP += 40;
			CureCountRemain--;
			return;
		}
		HP += 20;
		FoodCountRemain--;
	}

	public void ChangeATK(int arg) => ATKIndex = Math.Max(0, Math.Min(ATKIndex + arg, BufValues.Length - 1));

	public void ChangeDEF(int arg) => DEFIndex = Math.Max(0, Math.Min(DEFIndex + arg, BufValues.Length - 1));

	public void Rev(Player other)
	{
		ATKIndex = 12 - ATKIndex;
		DEFIndex = 12 - DEFIndex;
		other.ATKIndex = 12 - other.ATKIndex;
		other.DEFIndex = 12 - other.DEFIndex;
	}
	public void WZ(Player other)
	{
		ATKIndex = 6;
		DEFIndex = 6;
		other.ATKIndex = 6;
		other.DEFIndex = 6;
	}

	public void IncrementSkillChange() => SkillChangeRemain++;

	public Player Clone() => new(this);

	protected Player(Player original) : this(original.Ability)
	{
		_hp = original._hp;
		ATKIndex = original.ATKIndex;
		DEFIndex = original.DEFIndex;
		CurrentWord = original.CurrentWord;
		State = original.State;
		FoodCountRemain = original.FoodCountRemain;
		CureCountRemain = original.CureCountRemain;
		PoisonDmg = original.PoisonDmg;
		SeedTurnRemain = original.SeedTurnRemain;
		SkillChangeRemain = original.SkillChangeRemain;
	}
	#endregion

	public static implicit operator Player(PlayerData d) => new(Ability.Deserialize(d.AbilityIndex))
	{
		Name = d.Name,
		HP = d.HP, 
		ATKIndex = d.ATKIndex,
		DEFIndex = d.DEFIndex,
		CurrentWord = d.CurrentWord,
		State = d.State,
		FoodCountRemain = d.Args.FoodRem,
		CureCountRemain = d.Args.CureRem,
		PoisonDmg = d.Args.PoisonDmg,
		SeedTurnRemain = d.Args.SeedRem,
		SkillChangeRemain = d.Args.SkillRem
	};

	public class BredString
	{
		public string Name { get; init; } = string.Empty;
		public int Rep { get; private set; }
		public BredString(string name) => (Name, Rep) = (name, 0);
		public void Increment() => Rep++;
	}
}

[Flags]
public enum PlayerState
{
	Normal = 0,
	Poison = 1,
	Seed = 2
}
public static class PlayerStateEx
{
	public static string StateToString(this PlayerState state) => state switch
	{
		PlayerState.Normal => "なし",
		PlayerState.Poison => "毒",
		PlayerState.Seed => "やどりぎ",
		PlayerState.Poison | PlayerState.Seed => "どく、やどりぎ",
		_ => throw new ArgumentException($"PlayerState \"{state}\" has not been found.")
	};

}

public enum Proceeds { Random, True, False }
