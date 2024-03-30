namespace SBFirstLast4.Specialized.RevSimulator;

internal class GetDic
{
	private static readonly IEnumerable<Word> PlayWordBase = Words.TypedWords
		.Where(w => w.Contains(WordType.Play) && !w.IsHeal)
		.DistinctBy(w => w.Name);

	private static Dictionary<char, List<PlayWord>> PlayWords = [];

	private static Dictionary<char, int> GotouNum = [];
	private static Dictionary<char, int> GobiNum = [];

	internal static Dictionary<char, List<PlayWord>> GetDict(double allyATK, double allyDEF, double allyRand, double foeATK, double foeDEF, double foeRand)
	{
		PlayWords = [];
		GotouNum = [];
		GobiNum = [];
		LoadDict(allyATK, allyDEF, allyRand, foeATK, foeDEF, foeRand);
		SortDic();
		return PlayWords;
	}

	private static void LoadDict(double allyATK, double allyDEF, double allyRand, double foeATK, double foeDEF, double foeRand)
	{
		foreach (var i in PlayWordBase)
		{
			var allyDamage = SBTools.MaxDamage(i, allyATK, foeDEF, allyRand).MaxDamage;
			var foeDamage = SBTools.MaxDamage(i, allyDEF, foeATK, foeRand).MaxDamage;
			var word = new PlayWord(i, allyDamage, foeDamage);

			if (!PlayWords.TryGetValue(i.Start, out var words))
			{
				PlayWords[i.Start] = [word];
				continue;
			}
			words.Add(word);
		}

		foreach (var i in PlayWords.Keys)
			foreach (var j in PlayWords[i])
			{
				GotouNum[j.Word.End] = 0;
				GotouNum[i] = 0;
				GobiNum[j.Word.End] = 0;
				GobiNum[i] = 0;
			}

		foreach (var i in PlayWords.Keys)
			foreach (var j in PlayWords[i])
			{
				GotouNum[j.Word.Start]++;
				GobiNum[j.Word.End]++;
			}
	}

	private static void SortDic()
	{
		var yuusendo = new Dictionary<char, int>();
		foreach (var i in GotouNum.Keys)
			yuusendo[i] = 100;

		var sortedGotouNum = GotouNum.OrderBy(x => x.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
		var sortedGobiNum = GobiNum.OrderBy(x => x.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

		foreach (var (w, num) in sortedGotouNum)
		{
			if (num != 0)
				break;

			// 「ざ」とか遊びタイプがないとき
			// その語尾終わりの言葉があったら語頭の優先度を最強に
			if (sortedGobiNum[w] >= 1)
				yuusendo[w] = 0;
		}

		// 最大打点の最小値を記録(「ぶ」の枠を抽出)
		var syokiti = 1000;
		var minDamageDict = new Dictionary<char, int>();

		foreach (var i in Utils.KanaListCharSpread)
			minDamageDict[i] = syokiti;

		foreach (var i in PlayWords.Keys)
			foreach (var j in PlayWords[i])
			{
				var tmp = Math.Min(j.AllyDamage, j.FoeDamage);
				if (tmp < minDamageDict[j.Word.Start])
					minDamageDict[j.Word.Start] = tmp;
			}

		minDamageDict = minDamageDict.OrderBy(x => x.Value).ToDictionary(kv => kv.Key, kv => kv.Value);

		// 最小ダメージが51くらいだったら優先度を次に最強に
		foreach (var i in minDamageDict.Keys)
			if (minDamageDict[i] != syokiti && minDamageDict[i] >= 51)
				yuusendo[i] = 1;

		// 最後に(最大打点が51未満の(これでやったら順番微妙だったので素で))
		// 語頭の遊び単語の少なさで優先度付け(なんとなく強い気がするので)
		foreach (var (alp, i) in sortedGotouNum)
			if (yuusendo[alp] == 100)
				yuusendo[alp] = i + 2;

		// ソート実行
		foreach (var i in PlayWords.Keys)
			PlayWords[i] = [.. PlayWords[i].OrderBy(x => yuusendo[x.Word.End])];
	}
}

internal record PlayWord(Word Word, int AllyDamage, int FoeDamage)
{
	// デバッグ用
	public override string ToString() => $"{Word}[{AllyDamage}/{FoeDamage}]";
}

