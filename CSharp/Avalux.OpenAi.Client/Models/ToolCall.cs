using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class ToolCall
{
    [JsonPropertyName("id")] public required string Id { get; init; }

    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("arguments")] public required string Arguments { get; init; }
}