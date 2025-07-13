using Avalux.OpenAi.Protocol.Models;
using Avalux.OpenAi.Protocol.Schemas;

namespace Avalux.OpenAi.Protocol.Parsers
{
    public class Protocol_1_1_Parser : Protocol_1_0_Parser
    {
        protected override ProtocolEndpoint ParseEndpoint(string name, ApiEndpointSchema endpointSchema, Models.Protocol protocol)
        {
            var res = base.ParseEndpoint(name, endpointSchema, protocol);
            res.Prompts = endpointSchema.Prompts;
            return res;
        }
    }
}