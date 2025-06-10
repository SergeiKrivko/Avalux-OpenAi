using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class AiMessage
{
    [JsonPropertyName("role")] public string? Role { get; init; }

    [JsonPropertyName("content")] public string? Content { get; init; }

    [JsonPropertyName("tool_call_id")] public string? ToolCallId { get; init; }

    [JsonPropertyName("tool_calls")] public ToolCall[]? ToolCalls { get; init; }
}