using Buffer = System.Collections.Generic.List<(string Content, string Type)>;

namespace SBFirstLast4.Dynamic;

public class Procedure
{
	private readonly List<string> _lines = new();

	public void Push(string line) => _lines.Add(line.Trim());

	public async Task FlushAsync(Buffer _0, Action<string> _1, Func<Task> _2)
	{
		_lines.Clear();
		await Task.CompletedTask;
		throw new NotImplementedException();
	}
}
