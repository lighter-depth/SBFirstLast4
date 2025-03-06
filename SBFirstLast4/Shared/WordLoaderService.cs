namespace SBFirstLast4.Shared;

public interface IWordLoaderService
{
    Task LoadTL(int arg, HttpClient client);
    Task LoadTD(string arg, HttpClient client);
}

internal sealed class WordLoaderService : IWordLoaderService
{
    public async Task LoadTL(int arg, HttpClient client)
    {
        var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/tl-words/tl-words-{arg}.csv";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Accept-Charset", "utf-8");
        var response = await client.SendAsync(request);
        var resBodyStr = await response.Content.ReadAsStringAsync();
        var result = resBodyStr.TrimEnd().Split('\n');
        Words.NoTypeWords.AddRange(result);
    }

    public async Task LoadTD(string arg, HttpClient client)
    {
        var url = $"https://raw.githubusercontent.com/lighter-depth/DictionaryForSB/main/plain/typed-words-{arg}.csv";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Accept-Charset", "utf-8");

        var response = await client.SendAsync(request);

        var resBodyStr = await response.Content.ReadAsStringAsync();

        Words.TypedWords.AddRange(resBodyStr.Split('\n')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().Split())
            .Select(x => new Word(x.At(0), x.At(1), x.At(2))));
    }
}
