using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalux.OpenAi.Client.Models;

namespace Avalux.OpenAi.Client;

public class Client
{
    private readonly HttpClient _httpClient;
    public string BaseModel { get; set; } = "auto";

    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Client(Uri clientUri)
    {
        _httpClient = new HttpClient { BaseAddress = clientUri };
    }

    public int MaxToolCalls { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;

    private const string Url = "/api/v1/openai/request";

    private record Function(string Name, Func<string, object?, Task<object?>> Func, AiTool ToolDefinition);

    private readonly List<Function> _functions = [];

    public async Task<string?> SendTextRequestAsync<TIn>(string prompt, TIn request, object? context,
        RequestOptions? options = null)
    {
        var resp = await SendRequestAsync(prompt, request, context, options);
        return resp == null ? null : ProcessTextResult(resp);
    }

    public async Task<TOut?> SendJsonRequestAsync<TIn, TOut>(string prompt, TIn request, object? context,
        RequestOptions? options = null)
    {
        var resp = await SendRequestAsync(prompt, request, context, options);
        return resp == null ? default : ProcessJsonResult<TOut>(resp);
    }

    public async Task<string?> SendCodeRequestAsync<TIn>(string prompt, TIn request, object? context,
        RequestOptions? options = null)
    {
        var resp = await SendRequestAsync(prompt, request, context, options);
        return resp == null ? null : ProcessCodeResult(resp);
    }

    private async Task<string?> SendRequestAsync<TIn>(string prompt, TIn request, object? context,
        RequestOptions? options = null)
    {
        var resp = await SendInitAsync(prompt, request);
        for (int i = 0; i < MaxToolCalls; i++)
        {
            var retry = 0;
            while (true)
            {
                try
                {
                    var messages = resp.Messages.ToList();
                    var lastMessage = messages.Last();
                    if (lastMessage.ToolCalls == null || lastMessage.ToolCalls.Length == 0)
                    {
                        Console.WriteLine($"AI Result: {lastMessage.Content}");
                        if (lastMessage.Content == null)
                            throw new Exception("Invalid response from AI");
                        return lastMessage.Content;
                    }

                    resp = await SendAsync(new AiRequestModel
                    {
                        Messages = messages.Concat(await CallToolsAsync(lastMessage, context, options?.OnToolCalled)).ToArray(),
                        Tools = _functions.Select(f => f.ToolDefinition).ToArray(),
                        Model = options?.Model ?? BaseModel,
                    });
                    break;
                }
                catch (Exception)
                {
                    retry++;
                    if (retry > MaxRetries)
                        throw;
                    Console.WriteLine($"Retry ({retry}/{MaxRetries})...");
                }
            }
        }

        throw new Exception("Max calls reached");
    }

    private static string ProcessTextResult(string content)
    {
        return content;
    }

    private static T? ProcessJsonResult<T>(string content)
    {
        if (content.Contains("```json"))
        {
            content = content.Substring(
                content.IndexOf("```json", StringComparison.InvariantCulture) + "```json".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return JsonSerializer.Deserialize<T>(content);
    }

    private static string ProcessCodeResult(string content)
    {
        if (content.Contains("```"))
        {
            content = content.Substring(
                content.IndexOf("```", StringComparison.InvariantCulture) + "```".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return content;
    }

    private async Task<AiResponseModel> SendAsync(AiRequestModel request)
    {
        var content = JsonContent.Create(request);
        var resp = await _httpClient.PostAsync(Url, content);
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine(await resp.Content.ReadAsStringAsync());
            resp.EnsureSuccessStatusCode();
        }

        var result = await resp.Content.ReadFromJsonAsync<AiResponseModel>() ??
                     throw new Exception("Failed to send request");
        return result;
    }

    private async Task<AiResponseModel> SendInitAsync<TIn>(string prompt, TIn request, string? model = null)
    {
        var content = JsonContent.Create(new AiRequestModel
        {
            Messages =
            [
                new AiMessage
                {
                    Role = "system",
                    Content = prompt,
                },
                new AiMessage
                {
                    Role = "user",
                    Content = JsonSerializer.Serialize(request)
                },
            ],
            Tools = _functions.Select(f => f.ToolDefinition).ToArray(),
            Model = model ?? BaseModel,
        }, options: new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var resp = await _httpClient.PostAsync(Url, content);
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine(await resp.Content.ReadAsStringAsync());
            resp.EnsureSuccessStatusCode();
        }

        var result = await resp.Content.ReadFromJsonAsync<AiResponseModel>() ??
                     throw new Exception("Failed to send request");
        return result;
    }

    private async Task<List<AiMessage>> CallToolsAsync(AiMessage message, object? context,
        Action<ToolCallbackArgs>? onToolCalled = null)
    {
        var messages = new List<AiMessage>();
        if (message.ToolCalls == null)
            return messages;
        foreach (var toolCall in message.ToolCalls)
        {
            Console.WriteLine($"Calling tool {toolCall.Name}({toolCall.Arguments})");
            var function = _functions.Single(f => f.Name == toolCall.Name);
            object? res = null;
            Exception? exception = null;
            try
            {
                res = await function.Func(toolCall.Arguments, context);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in tool call: {toolCall.Name}: {e.Message}");
                exception = e;
            }

            if (onToolCalled != null)
                onToolCalled(new ToolCallbackArgs
                {
                    ToolName = function.Name,
                    ToolDescription = function.ToolDefinition.Description,
                    Parameter = toolCall.Arguments,
                    IsSuccess = exception == null,
                    Exception = exception,
                    Result = res,
                });

            messages.Add(new AiMessage
            {
                Role = "tool",
                Content = JsonSerializer.Serialize(res),
                ToolCallId = toolCall.Id,
            });
        }

        return messages;
    }

    public void AddFunction<TIn, TOut>(string name, Func<TIn?, object?, Task<TOut>> func, AiTool toolDefinition)
    {
        _functions.Add(new Function(name, async (data, context) =>
        {
            var param = JsonSerializer.Deserialize<TIn>(data);
            return await func(param, context);
        }, toolDefinition));
    }

    public void AddFunction<TIn, TContext, TOut>(string name, Func<TIn?, TContext?, Task<TOut>> func, AiTool toolDefinition)
    {
        _functions.Add(new Function(name, async (data, context) =>
        {
            var param = JsonSerializer.Deserialize<TIn>(data);
            return await func(param, (TContext?)context);
        }, toolDefinition));
    }

    public void AddFunction<TIn, TOut>(string name, Func<TIn?, Task<TOut>> func, AiTool toolDefinition)
    {
        _functions.Add(new Function(name, async (data, _) =>
        {
            var param = JsonSerializer.Deserialize<TIn>(data);
            return await func(param);
        }, toolDefinition));
    }
}