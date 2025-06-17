namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolTool
    {
        public string Name { get; set; }
        public ProtocolToolParameter[] Parameters { get; set; }
        public IProtocolType ResultType { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}