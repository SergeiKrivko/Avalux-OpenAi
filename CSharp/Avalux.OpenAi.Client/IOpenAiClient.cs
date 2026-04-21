using Avalux.OpenAi.Client.Models;

namespace Avalux.OpenAi.Client;

public interface IOpenAiClient
{
    public IChatCompletionUsage Usage { get; }

    public Task<IChatCompletion> CompleteAsync(ChatRequest request, CancellationToken ct = default);
    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, Task<TOut>> func);

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, Task<TOut>> func,
        BinaryData paramSchema);

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, CancellationToken, Task<TOut>> func);

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, CancellationToken, Task<TOut>> func,
        BinaryData paramSchema);
}

public interface IOpenAiClient<TContext> : IOpenAiClient
{
    public Task<IChatCompletion> CompleteAsync(ChatRequest request, TContext context, CancellationToken ct = default);
    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, TContext?, Task<TOut>> func);

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, TContext?, Task<TOut>> func,
        BinaryData paramSchema);

    public void AddFunction<TIn, TOut>(string name, string? description,
        Func<TIn, TContext?, CancellationToken, Task<TOut>> func);

    public void AddFunction<TIn, TOut>(string name, string? description,
        Func<TIn, TContext?, CancellationToken, Task<TOut>> func, BinaryData paramSchema);
}