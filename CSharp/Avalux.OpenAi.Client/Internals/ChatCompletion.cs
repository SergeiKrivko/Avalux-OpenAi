using System.Text.Json;

namespace Avalux.OpenAi.Client.Internals;

internal class ChatCompletion(
    OpenAI.Chat.ChatCompletion completion,
    JsonSerializerOptions jsonSerializerOptions,
    ChatCompletionUsage? usage = null) : IChatCompletion
{
    public string Content => completion.Content[0].Text;

    public IChatCompletionUsage Usage { get; } = usage ?? new ChatCompletionUsage(completion.Usage);

    public string ReadAsString()
    {
        return Content;
    }

    public string ReadAsCode()
    {
        var content = Content;
        if (content.Contains("```"))
        {
            content = content.Substring(
                content.IndexOf("```", StringComparison.InvariantCulture) + "```".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return content;
    }

    public string ReadAsCode(string language)
    {
        var content = Content;
        if (Content.Contains($"```{language}"))
        {
            content = content.Substring(
                content.IndexOf($"```{language}", StringComparison.InvariantCulture) + $"```{language}".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return content;
    }

    public T ReadAsJson<T>()
    {
        var content = Content;
        if (Content.Contains("```json"))
        {
            content = content.Substring(
                content.IndexOf("```json", StringComparison.InvariantCulture) + "```json".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return JsonSerializer.Deserialize<T>(content, jsonSerializerOptions) ?? throw new Exception("Invalid LLM response");
    }

    public object ReadAsJson(Type type)
    {
        var content = Content;
        if (Content.Contains("```json"))
        {
            content = content.Substring(
                content.IndexOf("```json", StringComparison.InvariantCulture) + "```json".Length);
            content = content.Substring(0, content.IndexOf("```", StringComparison.InvariantCulture));
        }

        return JsonSerializer.Deserialize(content, type, jsonSerializerOptions) ?? throw new Exception("Invalid LLM response");
    }
}