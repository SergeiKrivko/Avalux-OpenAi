namespace AiApiGenerator.Protocol.Models
{
    public class ProtocolTool
    {
        public string Name { get; set; }
        public IProtocolType InputType { get; set; }
        public IProtocolType OutputType { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}