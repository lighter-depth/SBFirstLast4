namespace SBFirstLast4.Minigames;

public abstract class ModelBase<TIn, TOut, TResult>(In<TIn> input, Out<TOut> output)
{
	protected In<TIn> _input = input;

	protected Out<TOut> _output = output;

	public abstract Task<TResult> RunAsync(CancellationToken token);
}


public delegate Task<TIn> In<TIn>();

public delegate Task Out<TOut>(TOut value);