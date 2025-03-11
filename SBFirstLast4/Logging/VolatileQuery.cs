using System.Net.Http.Json;
using System.Text.Json;

namespace SBFirstLast4.Logging;

public static class VolatileQuery
{
	internal static async Task<List<QueryResultModel>> GetAsync(HttpClient client, string roomID)
	{
		var url = $"https://sbfl4logging-lite.onrender.com/volatileQuery?roomID={roomID}";
		var response = await client.GetFromJsonAsync<List<QueryResultModel>>(url);
		return response ?? [];
	}

	internal static async Task PostAsync(HttpClient client, string userName, string roomID, string message)
	{
		var url = $"https://sbfl4logging-lite.onrender.com/volatileQuery?roomID={roomID}";
		var body = new
		{
			UserName = userName,
			RoomID = roomID,
			Message = message
		};
		var response = await client.PostAsJsonAsync(url, body);
		response.EnsureSuccessStatusCode();
	}

	internal static string Serialize(string message, DateTime timestamp) => JsonSerializer.Serialize(new
	{
		Message = message,
		Timestamp = timestamp
	});

	internal static QueryPayload? Deserialize(string payload) => JsonSerializer.Deserialize<QueryPayload>(payload);

}

internal sealed class QueryResultModel
{
	public string? Content { get; set; }

	public string? UserName { get; set; }
}

internal sealed class QueryPayload
{
	public string? Message { get; set; }

	public DateTime Timestamp { get; set; }
}