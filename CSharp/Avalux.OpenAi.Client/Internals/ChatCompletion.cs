using System.Text.Json;

namespace Avalux.OpenAi.Client.Internals;

internal class ChatCompletion(OpenAI.Chat.ChatCompletion completion) : IChatCompletion
{
    public string Content => completion.Content[0].Text;

    public IChatCompletionUsage Usage { get; } = new ChatCompletionUsage
    {
        InputTokens = completion.Usage.InputTokenCount,
        OutputTokens = completion.Usage.OutputTokenCount,
        TotalTokens = completion.Usage.TotalTokenCount,
    };

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

        return JsonSerializer.Deserialize<T>(content) ?? throw new Exception("Invalid LLM response");
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

        return JsonSerializer.Deserialize(content, type) ?? throw new Exception("Invalid LLM response");
    }
}