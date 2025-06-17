using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ApiToolParameter
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
        [JsonPropertyName("required")] public bool Required { get; set; }
    }
}