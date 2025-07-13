using System;

namespace Avalux.OpenAi.Protocol.Parsers
{
    public interface IProtocolParser
    {
        Version ProtocolVersion { get; }

        Models.Protocol Parse(string data, string defaultName);
        Models.Protocol ParseFile(string data);
    }
}