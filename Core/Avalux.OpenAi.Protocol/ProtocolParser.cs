using System;
using System.IO;
using System.Linq;
using Avalux.OpenAi.Protocol.Parsers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Avalux.OpenAi.Protocol
{
    public class ProtocolParser
    {
        private class ProtocolVersionSchema
        {
            public Version Version { get; set; }
        }

        private Version ReadVersion(string filePath)
        {
            var yaml = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var obj = deserializer.Deserialize<ProtocolVersionSchema>(yaml);
            return obj.Version;
        }

        private readonly IProtocolParser[] _parsers = new IProtocolParser[]
            {
                new Protocol_1_0_Parser(),
                new Protocol_1_1_Parser(),
            }
            .OrderBy(p => p.ProtocolVersion)
            .ToArray();

        public Models.Protocol ParseFile(string filePath)
        {
            var protocolVersion = ReadVersion(filePath);
            var parser = _parsers.FirstOrDefault(p => p.ProtocolVersion >= protocolVersion);
            return parser?.ParseFile(filePath);
        }
    }
}