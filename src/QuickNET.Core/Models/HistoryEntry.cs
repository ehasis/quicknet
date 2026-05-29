using System.Text.Json.Serialization;

namespace QuickNET.Models;

public record HistoryEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.Now;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "";

    [JsonPropertyName("input")]
    public string Input { get; init; } = "";

    [JsonPropertyName("output")]
    public string Output { get; init; } = "";

    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}
