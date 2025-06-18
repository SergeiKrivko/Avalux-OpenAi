using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class AiTool
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("parameters")] public ApiToolParameter[] Parameters { get; set; } = [];
}