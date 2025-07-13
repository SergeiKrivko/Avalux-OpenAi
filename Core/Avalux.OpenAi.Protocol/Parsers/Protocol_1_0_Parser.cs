﻿using System;
using System.IO;
using System.Linq;
using Avalux.OpenAi.Protocol.Models;
using Avalux.OpenAi.Protocol.Schemas;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Avalux.OpenAi.Protocol.Parsers
{
    public class Protocol_1_0_Parser : IProtocolParser
    {
        public virtual Version ProtocolVersion => new Version(1, 0);

        public virtual Models.Protocol Parse(string data, string defaultName)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var apiSchema = deserializer.Deserialize<ApiSchema>(data);

            var protocol = new Models.Protocol
            {
                Name = apiSchema.Name ?? defaultName,
                Version = Version.Parse(apiSchema.Version)
            };

            foreach (var item in apiSchema.Schemas)
            {
                protocol.CustomTypes[item.Key] = new ProtocolCustomType(item.Key, item.Value);
            }

            foreach (var protocolCustomType in protocol.CustomTypes.Values)
            {
                protocolCustomType.ResolveFields(protocol);
            }

            foreach (var item in apiSchema.Tools)
            {
                protocol.Tools.Add(ParseTool(item.Key, item.Value, protocol));
            }

            foreach (var item in apiSchema.Endpoints)
            {
                protocol.Endpoints.Add(ParseEndpoint(item.Key, item.Value, protocol));
            }

            ParseContextType(apiSchema, protocol);

            return protocol;
        }

        public Models.Protocol ParseFile(string path)
        {
            return Parse(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
        }

        protected virtual ProtocolTool ParseTool(string name, ApiToolSchema toolSchema, Models.Protocol protocol)
        {
            return new ProtocolTool
            {
                Name = name,
                Description = toolSchema.Description,
                Parameters = toolSchema.Params.Select(p => new ProtocolToolParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = protocol.ResolveType(p.Type),
                    Example = p.Example,
                }).ToArray(),
                ResultType = protocol.ResolveType(toolSchema.Result)
            };
        }

        protected virtual ProtocolEndpoint ParseEndpoint(string name, ApiEndpointSchema endpointSchema,
            Models.Protocol protocol)
        {
            ProtocolEndpointMode mode;
            switch (endpointSchema.Mode)
            {
                case "text":
                    mode = ProtocolEndpointMode.Text;
                    break;
                case "json":
                    mode = ProtocolEndpointMode.Json;
                    break;
                case "code":
                    mode = ProtocolEndpointMode.Code;
                    break;
                default:
                    throw new Exception($"Unsupported endpoint mode: {endpointSchema.Mode}");
            }

            var result = new ProtocolEndpoint
            {
                Name = name,
                InputType = protocol.ResolveType(endpointSchema.Input),
                OutputType = protocol.ResolveType(endpointSchema.Output),
                Mode = mode
            };
            if (result.Mode == ProtocolEndpointMode.Text && !result.OutputType.IsString)
                throw new Exception("'string' mode requires a 'string' output type");
            if (result.Mode == ProtocolEndpointMode.Code && !result.OutputType.IsString)
                throw new Exception("'code' mode requires a 'string' output type");

            return result;
        }

        protected virtual void ParseContextType(ApiSchema apiSchema, Models.Protocol protocol)
        {
            if (!string.IsNullOrWhiteSpace(apiSchema.Context))
                protocol.ContextType = protocol.ResolveType(apiSchema.Context);
        }
    }
}