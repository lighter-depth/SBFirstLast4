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
			new(b.UsedWords)
		);
}

public record PlayerData(string Name, int AbilityIndex, int HP, int ATKIndex, int DEFIndex, Word CurrentWord, PlayerState State, PlayerArgs Args)
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
}

public record PlayerArgs(int FoodRem, int CureRem, int PoisonDmg, int SeedRem, int SkillRem);
