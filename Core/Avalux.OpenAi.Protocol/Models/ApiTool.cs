using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ApiTool
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("parameters")] public ApiToolParameter[] Parameters { get; set; }
    }
}