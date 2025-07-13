using System;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolEndpoint
    {
        public string Name { get; set; }
        public IProtocolType InputType { get; set; }
        public IProtocolType OutputType { get; set; }
        public ProtocolEndpointMode Mode { get; set; }

        public string[] Prompts { get; set; } = Array.Empty<string>();

        public string ProcessPrompt(string source)
        {
            return source
                .Replace("${InputExample}", "```json\n" + InputType.JsonExample() + "\n```")
                .Replace("${OutputExample}", "```json\n" + OutputType.JsonExample() + "\n```")
                .Replace("${EndpointName}", Name);
        }
    }

    public enum ProtocolEndpointMode
    {
        Text,
        Json,
        Code,
    }
}