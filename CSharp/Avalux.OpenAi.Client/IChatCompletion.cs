namespace Avalux.OpenAi.Client;

public interface IChatCompletion
{
    public string Content { get; }
    public IChatCompletionUsage Usage { get; }

    public string ReadAsString();
    public string ReadAsCode();
    public string ReadAsCode(string language);
    public T ReadAsJson<T>();
    public object ReadAsJson(Type type);
}

public interface IChatCompletionUsage
{
    public int InputTokens { get; }
    public int OutputTokens { get; }
    public int TotalTokens { get; }
}