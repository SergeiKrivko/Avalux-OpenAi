namespace Avalux.OpenAi.Client.Internals;

internal class ChatCompletionUsage : IChatCompletionUsage
{
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int TotalTokens { get; private set; }
    public int TotalRequests { get; private set; }

    public ChatCompletionUsage()
    {
    }

    public ChatCompletionUsage(OpenAI.Chat.ChatTokenUsage usage)
    {
        InputTokens = usage.InputTokenCount;
        OutputTokens = usage.OutputTokenCount;
        TotalTokens = usage.TotalTokenCount;
        TotalRequests = 1;
    }

    public void Add(IChatCompletionUsage other)
    {
        InputTokens += other.InputTokens;
        OutputTokens += other.OutputTokens;
        TotalTokens += other.TotalTokens;
        TotalRequests += other.TotalRequests;
    }
}