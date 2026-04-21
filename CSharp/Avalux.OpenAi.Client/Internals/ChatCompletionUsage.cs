namespace Avalux.OpenAi.Client.Internals;

internal class ChatCompletionUsage : IChatCompletionUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    public void Add(IChatCompletionUsage other)
    {
        InputTokens += other.InputTokens;
        OutputTokens += other.OutputTokens;
        TotalTokens += other.TotalTokens;
    }
}