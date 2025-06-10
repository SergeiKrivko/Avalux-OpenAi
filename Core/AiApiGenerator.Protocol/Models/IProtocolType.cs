namespace AiApiGenerator.Protocol.Models
{
    public interface IProtocolType
    {
        bool IsString { get; }

        string JsonExample();
    }
}