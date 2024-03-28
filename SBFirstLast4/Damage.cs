using Skill = SBFirstLast4.WordType;

namespace SBFirstLast4;

public static class Damage
{
	public static int Calculate(Word attacker, Word receiver, double allyATK, double foeDEF, Skill skill, double random)
	{
		// 攻防倍率
		var statusEffect = allyATK / foeDEF;

		// 無属性同士 or 無属性 → 有属性
		if (attacker.IsEmpty)
		{
			// デバッガー
			if (skill == Skills.Debugger)
				return (int)(13 * (int)(statusEffect * 10) * 0.1);

			// 俺文字
			if (skill == Skills.Oremoji)
			{
				//　俺文字倍率
				var oremojiEffect = attacker.Length > 6 ? 2 : attacker.Length == 6 ? 1.5 : 1;

				return (int)(7 * (int)(10 * statusEffect) * oremojiEffect * 0.1);
			}

			return (int)(7 * (int)(10 * statusEffect) * 0.1);
		}

		// 特性倍率
		var skillEffect = SkillEffect(attacker, skill);

		// 急所倍率
		var doesCrit = DoesCrit(attacker, skill);
		var critEffect = doesCrit ? 1.5 : 1;

		// 急所時の処理
		if (doesCrit) statusEffect = Math.Max(1, statusEffect);

		// 有属性 → 無属性
		if (receiver.IsEmpty)
			return (int)((int)(10 * statusEffect) * skillEffect * critEffect);

		// タイプ相性
		var typeEffect = attacker.CalcEffectiveDmg(receiver);

		// 有属性 → 有属性
		return (int)((int)(10 * random * statusEffect * typeEffect) * skillEffect * critEffect);
	}

	public static int CalculateLow(Word attacker, Word receiver, double allyATK, double foeDEF, Skill skill)
		=> Calculate(attacker, receiver, allyATK, foeDEF, skill, 0.85);

	public static int CalculateHigh(Word attacker, Word receiver, double allyATK, double foeDEF, Skill skill)
		=> Calculate(attacker, receiver, allyATK, foeDEF, skill, 0.99);

	public static int Calculate(int baseDamage, double statusEffect, double skillEffect, double typeEffect, double critEffect, double random)
	{
		if (baseDamage < 10)
			return (int)(baseDamage * (int)(10 * statusEffect) * skillEffect * 0.1);

		return (int)((int)(baseDamage * random * statusEffect * typeEffect) * skillEffect * critEffect);
	}

	private static double SkillEffect(Word word, Skill skill) => skill switch
	{
		Skills.Oremoji when word.Length > 6 => 2,
		Skills.Oremoji when word.Length == 6 => 1.5,
		Skills.Global when word.Contains(Skills.Global) => 1.5,
		Skills.Jikken when word.Contains(Skills.Jikken) => 1.5,
		Skills.Kyojin when word.Contains(Skills.Kyojin) => 1.5,
		Skills.Shinkoushin when word.Contains(Skills.Shinkoushin) => 1.5,
		_ => 1
	};

	private static bool DoesCrit(Word word, Skill skill)
		=> (skill == Skills.Karate && word.Contains(Skills.Karate))
		|| (skill == Skills.Zuboshi && word.Contains(Skills.Zuboshi));
}

file static class Skills
{
	public const Skill
		Debugger = Skill.Normal,
		Hanshoku = Skill.Animal,
		Yadorigi = Skill.Plant,
		Global = Skill.Place,
		Jonetsu = Skill.Emote,
		RocknRoll = Skill.Art,
		Ikasui = Skill.Food,
		Mukimuki = Skill.Violence,
		Ishoku = Skill.Health,
		Karate = Skill.Body,
		Kachikochi = Skill.Mech,
		Jikken = Skill.Science,
		Sakinobashi = Skill.Time,
		Kyojin = Skill.Person,
		Busou = Skill.Work,
		Kasanegi = Skill.Cloth,
		Hoken = Skill.Society,
		Kakumei = Skill.Play,
		Dokubari = Skill.Bug,
		Keisan = Skill.Math,
		Zuboshi = Skill.Insult,
		Shinkoushin = Skill.Religion,
		Training = Skill.Sports,
		WZ = Skill.Weather,
		Oremoji = Skill.Tale;
}
