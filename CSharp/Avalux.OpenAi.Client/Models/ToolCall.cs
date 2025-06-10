using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class ToolCall
{
    [JsonPropertyName("id")] public required string Id { get; init; }

    [JsonPropertyName("type")] public string? Type { get; init; }

    [JsonPropertyName("function")] public required FunctionCall Function { get; init; }
}