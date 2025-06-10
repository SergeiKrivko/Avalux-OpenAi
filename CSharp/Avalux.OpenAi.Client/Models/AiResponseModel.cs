using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class AiResponseModel
{
    [JsonPropertyName("messages")] public AiMessage[] Messages { get; set; } = [];
}