namespace Avalux.OpenAi.Client.Models;

public class OpenAiClientOptions
{
    public required string Model { get; init; }
    public required string ApiKey { get; init; }
    public Uri? ApiEndpoint { get; init; }
    public bool Use { get; init; }
}