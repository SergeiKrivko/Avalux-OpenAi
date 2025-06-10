namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolEndpoint
    {
        public string Name { get; set; }
        public IProtocolType InputType { get; set; }
        public IProtocolType OutputType { get; set; }
        public ProtocolEndpointMode Mode { get; set; }
    }

    public enum ProtocolEndpointMode
    {
        Text,
        Json,
        Code,
    }
}