namespace Avalux.OpenAi.Client.Models;

public class ToolCallbackArgs
{
    public required string ToolName { get; init; }
    public string? ToolDescription { get; init; }
    public required string Parameter { get; init; }
    public object? Result { get; init; }
    public Exception? Exception { get; init; }
    public required bool IsSuccess { get; init; }
}