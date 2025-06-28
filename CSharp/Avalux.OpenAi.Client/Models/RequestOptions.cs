namespace Avalux.OpenAi.Client.Models;

public class RequestOptions
{
    public string Model { get; init; } = "auto";
    public Action<ToolCallbackArgs>? OnToolCalled { get; init; }
}