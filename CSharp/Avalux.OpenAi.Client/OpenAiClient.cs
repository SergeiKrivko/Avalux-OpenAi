using System.ClientModel;
using Avalux.OpenAi.Client.Internals;
using Avalux.OpenAi.Client.Models;
using OpenAI;
using OpenAI.Chat;
using ChatCompletion = Avalux.OpenAi.Client.Internals.ChatCompletion;

namespace Avalux.OpenAi.Client;

public class OpenAiClient<TContext>(OpenAiClientOptions options) : IOpenAiClient<TContext>
{
    private readonly ChatClient _client = new(options.Model, new ApiKeyCredential(options.ApiKey),
        new OpenAIClientOptions
        {
            Endpoint = options.ApiEndpoint
        });

    private readonly ChatCompletionUsage _usage = new();
    public IChatCompletionUsage Usage => _usage;

    private readonly Dictionary<string, IOpenAiTool<TContext>> _tools = [];

    public async Task<IChatCompletion> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        var messages = request.Messages.ToList();
        if (request.Options.Tools.Count == 0)
            foreach (var tool in _tools.Values)
                request.Options.Tools.Add(tool.Tool);
        var response = await _client.CompleteChatAsync(messages, request.Options, ct);
        var usage = new ChatCompletionUsage(response.Value.Usage);
        while (response.Value.FinishReason == ChatFinishReason.FunctionCall ||
               response.Value.FinishReason == ChatFinishReason.ToolCalls)
        {
            messages.Add(new AssistantChatMessage(response.Value.ToolCalls));
            foreach (var toolCall in response.Value.ToolCalls)
            {
                var tool = _tools[toolCall.FunctionName];
                var toolResult = await tool.Call(toolCall.FunctionArguments, default, ct);
                messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, toolResult));
            }

            response = await _client.CompleteChatAsync(messages, request.Options, ct);
            usage.Add(new ChatCompletionUsage(response.Value.Usage));
        }

        _usage.Add(usage);
        return new ChatCompletion(response.Value, usage);
    }

    public async Task<IChatCompletion> CompleteAsync(ChatRequest request, TContext context,
        CancellationToken ct = default)
    {
        var messages = request.Messages.ToList();
        if (request.Options.Tools.Count == 0)
            foreach (var tool in _tools.Values)
                request.Options.Tools.Add(tool.Tool);
        var response = await _client.CompleteChatAsync(messages, request.Options, ct);
        var usage = new ChatCompletionUsage(response.Value.Usage);
        while (response.Value.FinishReason == ChatFinishReason.FunctionCall ||
               response.Value.FinishReason == ChatFinishReason.ToolCalls)
        {
            messages.Add(new AssistantChatMessage(response.Value.ToolCalls));
            foreach (var toolCall in response.Value.ToolCalls)
            {
                var tool = _tools[toolCall.FunctionName];
                var toolResult = await tool.Call(toolCall.FunctionArguments, context, ct);
                messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, toolResult));
            }

            response = await _client.CompleteChatAsync(messages, request.Options, ct);
            usage.Add(new ChatCompletionUsage(response.Value.Usage));
        }

        _usage.Add(usage);
        return new ChatCompletion(response.Value, usage);
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, Task<TOut>> func)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, new BinaryData(typeof(TIn).ToSchema())),
            (@in, _, _) => func(@in)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, CancellationToken, Task<TOut>> func)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, new BinaryData(typeof(TIn).ToSchema())),
            (@in, _, ct) => func(@in, ct)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, TContext?, Task<TOut>> func)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, new BinaryData(typeof(TIn).ToSchema())),
            (@in, context, _) => func(@in, context)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description,
        Func<TIn, TContext?, CancellationToken, Task<TOut>> func)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, new BinaryData(typeof(TIn).ToSchema())),
            func));
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, Task<TOut>> func,
        BinaryData paramSchema)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, paramSchema),
            (@in, _, _) => func(@in)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, CancellationToken, Task<TOut>> func,
        BinaryData paramSchema)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, paramSchema),
            (@in, _, ct) => func(@in, ct)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description, Func<TIn, TContext?, Task<TOut>> func,
        BinaryData paramSchema)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, paramSchema),
            (@in, context, _) => func(@in, context)));
    }

    public void AddFunction<TIn, TOut>(string name, string? description,
        Func<TIn, TContext?, CancellationToken, Task<TOut>> func, BinaryData paramSchema)
    {
        _tools.Add(name, new OpenAiTool<TIn, TContext, TOut>(
            ChatTool.CreateFunctionTool(name, description, paramSchema),
            func));
    }
}