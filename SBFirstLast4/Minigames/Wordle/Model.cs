using In = SBFirstLast4.Minigames.In<string>;
using Out = SBFirstLast4.Minigames.Out<string>;

namespace SBFirstLast4.Minigames.Wordle;

public sealed class Model(In input, Out output) : ModelBase<string, string, string>(input, output)
{

	public override async Task<string> RunAsync(CancellationToken token)
	{
		return await Task.FromResult(string.Empty);
	}
}
