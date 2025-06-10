using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class AiRequestModel
{
    [JsonPropertyName("tools")] public string? Tools { get; init; }
    [JsonPropertyName("messages")] public AiMessage[] Messages { get; set; } = [];
    [JsonPropertyName("models")] public string[]? Models { get; set; }
}