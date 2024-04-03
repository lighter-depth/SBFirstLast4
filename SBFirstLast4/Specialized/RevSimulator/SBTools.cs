using MaxDamageKey = (SBFirstLast4.Word Word, double AllyATK, double FoeDEF, double Random, bool AllowViolence);

namespace SBFirstLast4.Specialized.RevSimulator;

internal static class SBTools
{
	internal static readonly Dictionary<char, Word[]> TypedWords = Words.TypedWords
		.Select(w => w with { Name = w.Name.Trim() })
		.Where(w => !string.IsNullOrWhiteSpace(w.Name))
		.DistinctBy(w => w.Name)
		.GroupBy(w => w.Name.At(0))
		.ToDictionary(g => g.Key, g => g.ToArray());

	private static readonly Dictionary<MaxDamageKey, MaxDamageInfo> MaxDamageCache = [];

	// 最大打点
	internal static MaxDamageInfo MaxDamage(Word word, double allyATK, double foeDEF, double random = 0.85, bool allowViolence = true)
	{
		if (MaxDamageCache.TryGetValue((word, allyATK, foeDEF, random, allowViolence), out var cachedResult))
			return cachedResult;

		if (word.IsEmpty)
		{
			static void ThrowException() => throw new ArgumentException("Word was empty.");
			ThrowException();
			return (-1, default, default);
		}

		var max_damage = 0;

		var output_word = Word.Default;

		var change_ability = WordType.Empty;

		var kashiramoji = word.End;

		var dtype1 = word.Type1;

		var status_effect = allyATK / foeDEF;
		foreach (var i in TypedWords[kashiramoji])
		{
			var selected_word = i.Name;

			if (Main.BannedWords.Contains(selected_word))
				continue;

			var atype1 = i.Type1;
			var atype2 = i.Type2;

			if (!allowViolence && (atype1 == WordType.Violence || atype2 == WordType.Violence))
				continue;

			// 急所すべきか
			var kyuusyo_subeki = false;

			if ((atype1 == WordType.Body || atype2 == WordType.Body) || (atype1 == WordType.Insult || atype2 == WordType.Insult))
			{
				if (selected_word.Length >= 7 && status_effect < 0.75)
					kyuusyo_subeki = true;

				if (selected_word.Length == 6 && status_effect < 1)
					kyuusyo_subeki = true;

				if (selected_word.Length <= 5)
					kyuusyo_subeki = true;
			}

			double ability_effect;
			WordType ability;

			if (i.Contains(WordType.Body) && kyuusyo_subeki)
			{
				ability_effect = 1.5;

				ability = WordType.Body;

				if (status_effect < 1) status_effect = 1;
			}

			else if (i.Contains(WordType.Insult) && kyuusyo_subeki)
			{
				ability_effect = 1.5;

				ability = WordType.Insult;

				if (status_effect < 1) status_effect = 1;
			}

			else if (selected_word.Length >= 7 && !kyuusyo_subeki)
			{
				ability_effect = 2;


				ability = WordType.Tale;
			}

			else if (selected_word.Length == 6)
			{
				ability_effect = 1.5;

				ability = WordType.Tale;
			}

			else if (i.Contains(WordType.Science))
			{
				ability_effect = 1.5;


				ability = WordType.Science;
			}

			else if (i.Contains(WordType.Place))
			{
				ability_effect = 1.5;

				ability = WordType.Place;
			}

			else if (i.Contains(WordType.Person))
			{
				ability_effect = 1.5;

				ability = WordType.Person;
			}
			else if (i.Contains(WordType.Religion))
			{
				ability_effect = 1.5;

				ability = WordType.Religion;
			}

			else
			{
				ability_effect = 1;

				ability = WordType.Empty;
			}

			if (dtype1 == WordType.Empty) random = 1;

			var damage = (int)((int)(10 * i.CalcEffectiveDmg(word) * status_effect * random) * ability_effect);

			if (damage > max_damage)
			{
				max_damage = damage;

				output_word = i;

				change_ability = ability;
			}
		}
		return MaxDamageCache[(word, allyATK, foeDEF, random, allowViolence)] = (max_damage, output_word, change_ability);
	}
}

internal readonly record struct MaxDamageInfo(int MaxDamage, Word OutputWord, WordType ChangeAbility)
{
	public static implicit operator MaxDamageInfo((int, Word, WordType) t) => new(t.Item1, t.Item2, t.Item3);
}