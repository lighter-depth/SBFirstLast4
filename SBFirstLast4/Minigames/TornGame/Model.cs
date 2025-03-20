using System.Diagnostics;

namespace SBFirstLast4.Minigames.TornGame;

public sealed class Model(int maxRound, In input, Out output)
{
	private readonly int _maxRound = maxRound;

	private readonly In _input = input;

	private readonly Out _output = output;

	public async Task<GameResult> RunAsync()
	{
		var totalWrongCount = 0;

		var stopwatch = new Stopwatch();

		stopwatch.Start();
		for (var i = 0; i < _maxRound; i++)
			totalWrongCount += await ExecuteRound(_input, _output);

		stopwatch.Stop();

		var elapsed = stopwatch.Elapsed;

		var score = GameRule.CalculateScore(totalWrongCount, elapsed.TotalMilliseconds);

		return new(score, elapsed);
	}

	private static async Task<int> ExecuteRound(In input, Out output)
	{
		var (category, word) = GameRule.GenerateWord();

		await output(word);

		var wrongCount = 0;
		while (true)
		{
			if (category == await input())
				break;

			wrongCount++;
		}

		return wrongCount;
	}
}

public delegate Task<WordCategory> In();

public delegate Task Out(string str);