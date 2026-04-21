using System.Text.Json;
using OpenAI.Chat;

namespace Avalux.OpenAi.Client.Internals;

internal class OpenAiTool<TIn, TContext, TOut>(ChatTool tool, Func<TIn, TContext?, CancellationToken, Task<TOut>> func)
    : IOpenAiTool<TContext>
{
    public ChatTool Tool => tool;

    public async Task<string> Call(string input, TContext? context, CancellationToken ct)
    {
        try
        {
            var inputObject = JsonSerializer.Deserialize<TIn>(input);
            if (inputObject == null)
                throw new Exception("Invalid input schema");
            var resultObject = await func(inputObject, context, ct);
            return JsonSerializer.Serialize(resultObject);
        }
        catch (Exception e)
        {
            return JsonSerializer.Serialize(new ErrorResponseObject(e.Message));
        }
    }

    public async Task<string> Call(BinaryData input, TContext? context, CancellationToken ct)
    {
        try
        {
            var inputObject = JsonSerializer.Deserialize<TIn>(input);
            if (inputObject == null)
                throw new Exception("Invalid input schema");
            var resultObject = await func(inputObject, context, ct);
            return JsonSerializer.Serialize(resultObject);
        }
        catch (Exception e)
        {
            return JsonSerializer.Serialize(new ErrorResponseObject(e.Message));
        }
    }

    private record ErrorResponseObject(string Error);
}