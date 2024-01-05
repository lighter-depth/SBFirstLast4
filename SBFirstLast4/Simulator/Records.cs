using System.Collections;
using System.Text;

namespace SBFirstLast4.Simulator;

public record BattleData(PlayerData Player1, PlayerData Player2, PlayerData PreActor, PlayerData PreReceiver, bool IsPlayer1sTurn, int TurnNum, List<string> UsedWords)
{
	public static explicit operator BattleData(Battle b) =>
		new
		(
			b.Player1,
			b.Player2,
			b.PreActor,
			b.PreReceiver,
			b.IsPlayer1sTurn,
			b.TurnNum,
			b.UsedWords.Take(..^1).ToList()
		);

	public Word CurrentWord => IsPlayer1sTurn ? Player1.CurrentWord : Player2.CurrentWord;

	public bool IsEdited { get; set; }
	public static BattleData Default { get; } = new(PlayerData.Default, PlayerData.Default, PlayerData.Default, PlayerData.Default, default, 0, new());

	public override string ToString() => Serialize();

	public string Serialize()
	{
		var sb = new StringBuilder();
		sb.Append("###");
		sb.Append("/b/");
		sb.Append(Player1.Serialize());
		sb.Append("/b/");
		sb.Append(Player2.Serialize());
		sb.Append("/b/");
		sb.Append(PreActor.Serialize());
		sb.Append("/b/");
		sb.Append(PreReceiver.Serialize());
		sb.Append("/b/");
		sb.Append(IsPlayer1sTurn);
		sb.Append("/b/");
		sb.Append(TurnNum);
		sb.Append("###");
		return sb.ToString();
	}

	public static BattleData Deserialize(string str, List<string> usedWords)
	{
		if (!str.StartsWith("###") || !str.EndsWith("###") || str.Length < 4)
			return Default;

		var data = str[3..^3].Split("/b/");

		return new
		(
			Player1: PlayerData.Deserialize(data.At(0)),
			Player2: PlayerData.Deserialize(data.At(1)),
			PreActor: PlayerData.Deserialize(data.At(2)),
			PreReceiver: PlayerData.Deserialize(data.At(3)),
			IsPlayer1sTurn: Bool.ParseOrDefault(data.At(4)),
			TurnNum: Int.ParseOrDefault(data.At(5)),
			UsedWords: usedWords
		);
	}
}

public record PlayerData(string Name, int AbilityIndex, int HP, int ATKIndex, int DEFIndex, Word CurrentWord, PlayerState State, PlayerArgs Args) : IEnumerable<string>
{
	public static implicit operator PlayerData(Player p) =>
		new
		(
			p.Name,
			p.Ability.Serialize(),
			p.HP,
			p.ATKIndex,
			p.DEFIndex,
			p.CurrentWord,
			p.State,
			new
			(
				p.FoodCountRemain,
				p.CureCountRemain,
				p.PoisonDmg,
				p.SeedTurnRemain,
				p.SkillChangeRemain
			)
		);

	public static PlayerData Default { get; } = new(string.Empty, 0, 0, 0, 0, Word.Default, default, PlayerArgs.Default);

	public string Serialize()
	{
		var sb = new StringBuilder();
		sb.Append("||");
		sb.Append(this.Stringify("/p/"));
		sb.Append("||");
		return sb.ToString();
	}

	public static PlayerData Deserialize(string? str)
	{
		if (str is null)
			return Default;

		str = str.Trim();

		if (!str.StartsWith("||") || !str.EndsWith("||") || str.Length < 3)
			return Default;

		var data = str[2..^2].Split("/p/");

		return new
		(
			Name: data.At(0) ?? string.Empty,
			AbilityIndex: Int.ParseOrDefault(data.At(1)),
			HP: Int.ParseOrDefault(data.At(2)),
			ATKIndex: Int.ParseOrDefault(data.At(3)),
			DEFIndex: Int.ParseOrDefault(data.At(4)),
			CurrentWord: Word.Deserialize(data.At(5)),
			State: (PlayerState)Int.ParseOrDefault(data.At(6)),
			Args: new
			(
				FoodRem: Int.ParseOrDefault(data.At(7)),
				CureRem: Int.ParseOrDefault(data.At(8)),
				PoisonDmg: Int.ParseOrDefault(data.At(9)),
				SeedRem: Int.ParseOrDefault(data.At(10)),
				SkillRem: Int.ParseOrDefault(data.At(11))
			)
		);
	}

	public IEnumerator<string> GetEnumerator()
	{
		yield return Name;
		yield return AbilityIndex.ToString();
		yield return HP.ToString();
		yield return ATKIndex.ToString();
		yield return DEFIndex.ToString();
		yield return CurrentWord.Serialize();
		yield return ((int)State).ToString();
		foreach (var i in Args)
			yield return i.ToString();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public record PlayerArgs(int FoodRem, int CureRem, int PoisonDmg, int SeedRem, int SkillRem) : IEnumerable<int>
{
	public static PlayerArgs Default { get; } = new(0, 0, 0, 0, 0);

	public IEnumerator<int> GetEnumerator()
	{
		yield return FoodRem;
		yield return CureRem;
		yield return PoisonDmg;
		yield return SeedRem;
		yield return SkillRem;
	}


	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

file static class Int
{
	internal static int ParseOrDefault(string? str) => int.TryParse(str, out var val) ? val : default;
}

file static class Bool 
{
	internal static bool ParseOrDefault(string? str) => bool.TryParse(str, out var val) && val;
}
