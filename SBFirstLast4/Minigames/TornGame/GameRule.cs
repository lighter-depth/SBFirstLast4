namespace SBFirstLast4.Minigames.TornGame;

public static class GameRule
{
	private static readonly Random Random = new();

	private const int CategoryCount = 5;

	private const int MaxScore = 100000;

	public static WordData GenerateWord()
	{
		var category = (WordCategory)Random.Next(CategoryCount);

		return new(category, category switch
		{
			WordCategory.TD => RandomValue(Words.TWords),
			WordCategory.TL_TL => RandomValue(Words.NNWords),
			WordCategory.TL_TD => RandomValue(Words.NTWords),
			WordCategory.TD_TL => RandomValue(Words.TNWords),
			WordCategory.TD_TD => RandomValue(Words.TTWords),
			_ => RandomValue(Words.TWords)
		});
	}

	public static int CalculateScore(int totalWrongCount, double elapsedMilliseconds)
	{
		var wrongAttemptsPenalty = totalWrongCount * 1000;
		var timePenalty = (int)(elapsedMilliseconds / 10);
		var score = MaxScore - wrongAttemptsPenalty - timePenalty;
		return Math.Max(score, 0);
	}

	private static string RandomValue(string[] array) => array[Random.Next(array.Length)];
}

public readonly record struct WordData(WordCategory Category, string Word);

public readonly record struct GameResult(int Score, TimeSpan Elapsed);

public enum WordCategory
{
	TD, TL_TL, TL_TD, TD_TL, TD_TD
}
