using System.Text.Json.Serialization;

namespace Avalux.OpenAi.Client.Models;

public class FunctionCall
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("arguments")] public required string Arguments { get; init; }
}