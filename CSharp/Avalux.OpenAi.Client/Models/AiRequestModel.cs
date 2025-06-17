using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class AiRequestModel
{
    [JsonPropertyName("tools")] public AiTool[] Tools { get; init; } = [];
    [JsonPropertyName("messages")] public AiMessage[] Messages { get; set; } = [];
    [JsonPropertyName("model")] public string? Model { get; set; }
}