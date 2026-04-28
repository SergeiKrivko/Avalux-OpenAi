using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using OpenAI.Chat;
using Avalux.OpenAi.Client.Internals;

namespace Avalux.OpenAi.Client.Models;

public class ChatRequest
{
    internal readonly List<ChatMessage> Messages = [];
    internal readonly ChatCompletionOptions Options = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public ChatRequest AddSystemPrompt(string content)
    {
        Messages.Add(new SystemChatMessage(content));
        return this;
    }

    public ChatRequest AddUserMessage(string content)
    {
        Messages.Add(new UserChatMessage(content));
        return this;
    }

    public ChatRequest AddUserMessage(object content)
    {
        Messages.Add(new UserChatMessage(JsonSerializer.Serialize(content, _jsonSerializerOptions)));
        return this;
    }

    public ChatRequest AddUserMessage(IEnumerable<ChatMessageContentPart> parts)
    {
        Messages.Add(new UserChatMessage(parts));
        return this;
    }

    public ChatRequest AddUserMessage(params ChatMessageContentPart[] parts)
    {
        Messages.Add(new UserChatMessage(parts));
        return this;
    }

    public ChatRequest AddAssistantMessage(string content)
    {
        Messages.Add(new AssistantChatMessage(content));
        return this;
    }

    public ChatRequest AddAssistantMessage(object content)
    {
        Messages.Add(new AssistantChatMessage(JsonSerializer.Serialize(content, _jsonSerializerOptions)));
        return this;
    }

    public ChatRequest AddAssistantMessage(IEnumerable<ChatMessageContentPart> parts)
    {
        Messages.Add(new AssistantChatMessage(parts));
        return this;
    }

    public ChatRequest AddAssistantMessage(params ChatMessageContentPart[] parts)
    {
        Messages.Add(new AssistantChatMessage(parts));
        return this;
    }

    public ChatRequest SetResponseType<T>()
    {
        return SetResponseType(typeof(T));
    }

    public ChatRequest SetResponseType(Type type)
    {
        return SetResponseType(type.Name, type.ToSchema());
    }

    public ChatRequest SetResponseType(string name, TypeSchema type)
    {
        return SetResponseType(name, new BinaryData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(type))));
    }

    public ChatRequest SetResponseType(string name, BinaryData data)
    {
        // Options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(name, data, jsonSchemaIsStrict: true);
        var index = Messages.FindLastIndex(e => e is SystemChatMessage) + 1;
        Messages.Insert(index,
            new SystemChatMessage($"Final response must be presented as a json schema {name}. Description in the OpenAPI format:\n\n```json\n{data}\n```"));
        return this;
    }
}