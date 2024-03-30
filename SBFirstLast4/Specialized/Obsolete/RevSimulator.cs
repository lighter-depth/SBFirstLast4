using Microsoft.JSInterop;
using SBFirstLast4.Simulator;
using ChainElement = (SBFirstLast4.Word Word, int AllyHP, int FoeHP);
using StrategyElement = (bool HasWon, System.Collections.Generic.List<int> Strategy, System.Collections.Generic.List<(SBFirstLast4.Word Word, int AllyHP, int FoeHP)> Chain);
using MaxDamageResult = (int MaxDamage, SBFirstLast4.Word Word, SBFirstLast4.WordType Ability);
using MaxDamageKey = (SBFirstLast4.Word Word, double AllyATK, double FoeDEF, double Random, bool AllowViolence);
using InitialConfig = (int InitialHP, int InitialATK, int InitialDEF, double Random, double? Depth);

namespace SBFirstLast4.Specialized.Obsolete;

public class RevSimulator
{
	private static readonly Dictionary<char, Word[]> PlayWords = Words.TypedWords
				.Where(w => w.Contains(WordType.Play) && !w.IsHeal)
				.ToLookup(w => w.Start)
				.ToDictionary(g => g.Key, g => g.ToArray());

	private readonly string FirstWord = string.Empty;
	private readonly WordType SecondType;

	private InitialConfig Ally = (60, 6, 6, 0.90, null);
	private InitialConfig Foe = (60, 6, 6, 0.90, null);

	private readonly int MaxChain = 100;
	private readonly int MaxAttempt = 10;

	private int AllyATKIndex = 6;
	private double AllyATK => Player.BufValues[AllyATKIndex];

	private int AllyDEFIndex = 6;
	private double AllyDEF => Player.BufValues[AllyDEFIndex];

	private int FoeATKIndex = 6;
	private double FoeATK => Player.BufValues[FoeATKIndex];

	private int FoeDEFIndex = 6;
	private double FoeDEF => Player.BufValues[FoeDEFIndex];

	private double AllyStatusEffect => AllyATK / FoeDEF;
	private double FoeStatusEffect => FoeATK / AllyDEF;

	private Word WordBuffer = Word.Default;

	internal List<StrategyElement> Strategies { get; set; } = [];

	private static readonly Dictionary<MaxDamageKey, MaxDamageResult> MaxDamageDictionary = [];
	private static readonly Dictionary<char, Word[]> _killerCandidates = [];
	private static Word[] KillerCandidates(char start)
		=> _killerCandidates.TryGetValue(start, out var words) ? words : _killerCandidates[start] = Words.TypedWords.Where(w => w.Start == start && !w.IsHeal && !w.Contains(WordType.Normal)).ToArray();

	internal void Run()
	{
		WordBuffer = new(FirstWord, WordType.Play, SecondType);

		var originalWord = WordBuffer;

		var depthGen = new Random();

		var strategies = new List<StrategyElement>();

		var depthCount = 0;

		var oldStrategy = new List<int>();

		while (depthCount < MaxAttempt)
		{
			List<ChainElement> chain = [(originalWord, Ally.InitialHP, Foe.InitialHP)];
			WordBuffer = originalWord;
			var (allyHP, foeHP) = (Ally.InitialHP, Foe.InitialHP);
			(AllyATKIndex, AllyDEFIndex, FoeATKIndex, FoeDEFIndex) = (Ally.InitialATK, Ally.InitialDEF, Foe.InitialATK, Foe.InitialDEF);
			var isPlayer1sTurn = true;
			var strategy = new List<int>();
			var chainCount = 0;
			while (chainCount < MaxChain)
			{
				if (!PlayWords.TryGetValue(WordBuffer.End, out var playWords) || playWords.Length == 0)
					break;

				var countMap = new Dictionary<Word, int>();

				foreach (var word in playWords)
				{
					if (chain.Any(c => c.Word.Name == word.Name))
						continue;

					if (isPlayer1sTurn && allyHP - MaxDamage(word, FoeATK, AllyDEF, Foe.Random, true).MaxDamage <= 0)
						continue;

					if (!isPlayer1sTurn && foeHP - MaxDamage(word, AllyATK, FoeDEF, Ally.Random, true).MaxDamage <= 0)
						continue;

					if (!PlayWords.TryGetValue(word.End, out var nextWords))
						countMap.Add(word, 0);

					else
						countMap.Add(word, nextWords.Length);
				}

				if (countMap.Count == 0)
					break;

				var candidatesBeforeFiltered = countMap.OrderBy(kv => kv.Value);

				KeyValuePair<Word, int>[] candidates;

				if (Ally.Depth is not null && isPlayer1sTurn)
					candidates = candidatesBeforeFiltered.Where(kv => kv.Value < Ally.Depth).ToArray();
				else if (Foe.Depth is not null && !isPlayer1sTurn)
					candidates = candidatesBeforeFiltered.Where(kv => kv.Value < Foe.Depth).ToArray();
				else
					candidates = [..candidatesBeforeFiltered];

				int depth;
				if (candidates[0].Value == 0) depth = 0;
				else do depth = depthGen.Next(candidates.Length);
					while (oldStrategy.SequenceEqual<int>([.. strategy, depth]));

				var nextWord = candidates[depth].Key;

				if (isPlayer1sTurn) foeHP -= Damage(nextWord, WordBuffer, AllyStatusEffect, Ally.Random);
				else allyHP -= Damage(nextWord, WordBuffer, FoeStatusEffect, Foe.Random);

				WordBuffer = nextWord;
				strategy.Add(depth);
				chain.Add((WordBuffer, allyHP, foeHP));
				chainCount++;
				isPlayer1sTurn = !isPlayer1sTurn;
				Rev();
			}
			strategies.Add((!isPlayer1sTurn, strategy, chain));
			oldStrategy = strategy;
			depthCount++;
		}
		Strategies = strategies;
	}

	private static int Damage(Word attacker, Word receiver, double statusEffect, double random)
		=> (int)(10 * attacker.CalcEffectiveDmg(receiver) * statusEffect * (receiver.IsEmpty ? 1 : random));

	private void Rev()
	{
		AllyATKIndex = 12 - AllyATKIndex;
		AllyDEFIndex = 12 - AllyDEFIndex;
		FoeATKIndex = 12 - FoeATKIndex;
		FoeDEFIndex = 12 - FoeDEFIndex;
	}

	private static MaxDamageResult MaxDamage(Word word, double allyATK, double foeDEF, double random, bool allowViolence)
	{
		if (MaxDamageDictionary.TryGetValue((word, allyATK, foeDEF, random, allowViolence), out var result))
			return result;

		var (maxDamage, outputWord, changeAbility) = (0, Word.Default, WordType.Empty);
		var start = word.End;

		if (word.IsEmpty) random = 1;

		var candidates = KillerCandidates(start);

		foreach (var i in candidates)
		{
			var statusEffect = allyATK / foeDEF;

			if (!allowViolence && (i.Contains(WordType.Violence))) continue;

			var doesCrit = i.IsCritable && ((i.Length > 6 && statusEffect < 0.75) || (i.Length == 6 && statusEffect < 1) || i.Length < 6);

			double abilityEffect;
			WordType ability;

			if (i.Contains(WordType.Body) && doesCrit) (abilityEffect, ability, statusEffect) = (1.5, WordType.Body, Math.Max(statusEffect, 1));
			else if (i.Contains(WordType.Insult) && doesCrit) (abilityEffect, ability, statusEffect) = (1.5, WordType.Insult, Math.Max(statusEffect, 1));
			else if (i.Length > 6 && !doesCrit) (abilityEffect, ability) = (2, WordType.Tale);
			else if (i.Length == 6) (abilityEffect, ability) = (1.5, WordType.Tale);
			else if (i.Contains(WordType.Science)) (abilityEffect, ability) = (1.5, WordType.Science);
			else if (i.Contains(WordType.Place)) (abilityEffect, ability) = (1.5, WordType.Place);
			else if (i.Contains(WordType.Person)) (abilityEffect, ability) = (1.5, WordType.Person);
			else if (i.Contains(WordType.Religion)) (abilityEffect, ability) = (1.5, WordType.Religion);
			else (abilityEffect, ability) = (1, WordType.Empty);

			var damage = (int)((int)(10 * i.CalcEffectiveDmg(word) * statusEffect * random) * abilityEffect);

			if (damage > maxDamage) (maxDamage, outputWord, changeAbility) = (damage, i, ability);
		}

		return MaxDamageDictionary[(word, allyATK, foeDEF, random, allowViolence)] = (maxDamage, outputWord, changeAbility);
	}
}
