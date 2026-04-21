using OpenAI.Chat;

namespace Avalux.OpenAi.Client.Internals;

internal interface IOpenAiTool<TContext>
{
    public ChatTool Tool { get; }
    public Task<string> Call(string input, TContext? context, CancellationToken ct);
    public Task<string> Call(BinaryData input, TContext? context, CancellationToken ct);
}