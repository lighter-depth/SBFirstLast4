using StatusInfo = (int HP, double ATK, double DEF, double Random);

namespace SBFirstLast4.Specialized.RevSimulator;

internal static class Main
{
	internal static HashSet<string> BannedWords { get; private set; } = [];
	private static Dictionary<char, List<PlayWord>> PlayWords = [];

	private static double AllyRandom = 0.85;
	private static double FoeRandom = 0.85;

	private static double AllyStatusEffect = 1;
	private static double FoeStatusEffect = 1;

	private static readonly Dictionary<(Word, StatusInfo, StatusInfo, int), (List<TurnInfo> Value, HashSet<string> BannedWords)> Cache = [];

	// ここで設定-----
	internal static List<TurnInfo> EntryPoint(Word firstWord, StatusInfo ally, StatusInfo foe, int maxLength, HashSet<string> bannedWords, CancellationToken token = default)
	{
		if (Cache.TryGetValue((firstWord, ally, foe, maxLength), out var cache) && cache.BannedWords.SetEquals(bannedWords))
			return cache.Value;

		AllyRandom = ally.Random;
		FoeRandom = foe.Random;

		// 禁止ワード一覧設定
		BannedWords = bannedWords;

		AllyStatusEffect = ally.ATK / foe.DEF;
		FoeStatusEffect = ally.DEF / foe.ATK;

		PlayWords = GetDic.GetDict(ally.ATK, ally.DEF, ally.Random, foe.ATK, foe.DEF, foe.Random);

		var result = PlayerTurn(new(firstWord, 1, 1), true, (ally.HP, foe.HP), [], maxLength, token: token);
		Cache[(firstWord, ally, foe, maxLength)] = (result, bannedWords);
		return result;
	}

	private static List<TurnInfo> PlayerTurn(PlayWord word, bool isPlayer1Turn, (int Ally, int Foe) hp, List<TurnInfo> chain, int maxLen, int saiki = 1, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		chain = [.. chain, (word.Word, hp.Ally, hp.Foe)];

		// 「ざ」など続く単語がなかったら終了
		if (!PlayWords.TryGetValue(chain.LastOrDefault().Word.End, out var playWords))
			return chain;

		// 勝てる時用の最短チェインを保存
		var bestChain = chain;

		// 負ける時用の最大チェインを保存
		var longestChain = chain;

		var (player1_HP_now_org, player2_HP_now_org) = hp;

		// 例えば「ういいれ」で最大長さ4って分かった後に「うーむ」で5以上探すの無駄だよねっていうやつ
		var max = maxLen;

		foreach (var @new in playWords)
		{
			var (player1_HP_now, player2_HP_now) = (player1_HP_now_org, player2_HP_now_org);

			// 今まで見つかった最短よりも長くなったら無視でok
			if (bestChain.Count > max)
				break;

			// 「～ど→どらいぶ」みたいに1手で詰みとれたら最短確定だよね
			if (bestChain.Count - chain.Count == 1)
				break;

			// 今までに使用してたら飛ばす(禁止単語設定)
			if (BannedWords.Contains(@new.Word.Name))
				continue;

			// 単語が使用済みなら飛ばす
			if (chain.Any(c => c.Word.Name == @new.Word.Name))
				continue;

			// HP足りなくても飛ばす
			if (isPlayer1Turn && player1_HP_now <= @new.FoeDamage)
				continue;

			if (!isPlayer1Turn && player2_HP_now <= @new.AllyDamage)
				continue;

			// タイプありorなしで乱数設定
			double ransuu;
			if (word.Word.IsEmpty) ransuu = 1;
			else if (isPlayer1Turn) ransuu = AllyRandom;
			else ransuu = FoeRandom;

			// ダメージ計算
			if (isPlayer1Turn)
				player2_HP_now = player2_HP_now_org - (int)(10 * @new.Word.CalcEffectiveDmg(word.Word) * AllyStatusEffect * ransuu);
			else
				player1_HP_now = player1_HP_now_org - (int)(10 * @new.Word.CalcEffectiveDmg(word.Word) * FoeStatusEffect * ransuu);

			// 再帰
			var newChain = PlayerTurn(@new, !isPlayer1Turn, (player1_HP_now, player2_HP_now), chain, max, saiki + 1, token);

			// 先攻が勝てるとき
			if (isPlayer1Turn && newChain.Count % 2 == 0)
			{
				// 初期値のとき更新
				if (bestChain.SequenceEqual(chain))
				{
					bestChain = newChain;
					if (max > bestChain.Count) max = bestChain.Count;
				}
				// 1回以上更新されたとき
				// 以前のチェインよりも短く詰ませられたら更新
				else if (bestChain.Count > newChain.Count)
				{
					bestChain = newChain;
					if (max > bestChain.Count) max = bestChain.Count;
				}
			}

			// 後攻が勝てるとき
			else if (!isPlayer1Turn && newChain.Count % 2 == 1)
			{
				if (bestChain.SequenceEqual(chain))
				{
					bestChain = newChain;
					if (max > bestChain.Count) max = bestChain.Count;
				}
				else if (bestChain.Count > newChain.Count)
				{
					bestChain = newChain;
					if (max > bestChain.Count) max = bestChain.Count;
				}
			}

			// 負ける時用に最長のチェイン保持
			if (newChain.Count > longestChain.Count) longestChain = newChain;
		}

		// 1回も更新がなかったら(1回も勝てなかったら)最長を返す
		if (bestChain.SequenceEqual(chain))
			return longestChain;

		// 勝てたらベストを返す
		else
			return bestChain;
	}
}

internal readonly record struct TurnInfo(Word Word, int AllyHP, int FoeHP)
{
	public static implicit operator TurnInfo((Word, int, int) t) => new(t.Item1, t.Item2, t.Item3);
}