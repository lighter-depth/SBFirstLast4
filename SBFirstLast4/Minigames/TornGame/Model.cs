using System.Diagnostics;
using In = SBFirstLast4.Minigames.In<SBFirstLast4.Minigames.TornGame.WordCategory>;
using Out = SBFirstLast4.Minigames.Out<string>;

namespace SBFirstLast4.Minigames.TornGame;

public sealed class Model(int maxRound, In input, Out output) : ModelBase<WordCategory, string, GameResult>(input, output)
{
	private readonly int _maxRound = maxRound;

	public override async Task<GameResult> RunAsync(CancellationToken token)
	{
		var totalWrongCount = 0;

		var stopwatch = new Stopwatch();

		stopwatch.Start();
		for (var i = 0; i < _maxRound; i++)
			totalWrongCount += await ExecuteRound(_input, _output, token);

		if (token.IsCancellationRequested)
			return default;

		stopwatch.Stop();

		var elapsed = stopwatch.Elapsed;

		var score = GameRule.CalculateScore(totalWrongCount, elapsed.TotalMilliseconds);

		return new(score, elapsed);
	}

	private static async Task<int> ExecuteRound(In input, Out output, CancellationToken token)
	{
		var (category, word) = GameRule.GenerateWord();

		if (token.IsCancellationRequested)
			return default;

		await output(word);

		var wrongCount = 0;
		while (true)
		{
			if (token.IsCancellationRequested)
				break;

			if (category == await input())
				break;

			wrongCount++;
		}

		return wrongCount;
	}
}