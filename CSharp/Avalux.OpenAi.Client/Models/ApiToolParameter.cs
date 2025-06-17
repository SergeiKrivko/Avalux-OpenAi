using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class ApiToolParameter
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("format")] public string? Format { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
}