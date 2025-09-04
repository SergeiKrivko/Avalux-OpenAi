using System;
using System.Linq;
using System.Text.Json;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolTool
    {
        public string Name { get; set; }
        public ProtocolToolParameter[] Parameters { get; set; } = Array.Empty<ProtocolToolParameter>();
        public IProtocolType ResultType { get; set; }
        public string Description { get; set; } = string.Empty;

        public string GenerateJsonDefinition()
        {
            var res = new ApiTool
            {
                Name = Name,
                Description = Description,
                Parameters = Parameters.Select(p => p.ToApiParameter()).ToArray()
            };
            return JsonSerializer.Serialize(res);
        }
    }
}