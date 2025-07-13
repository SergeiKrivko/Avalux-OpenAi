﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Avalux.OpenAi.Protocol.Models
{
    public class Protocol
    {
        public string Name { get; set; }
        public Version Version { get; set; }

        public Dictionary<string, ProtocolCustomType> CustomTypes { get; } =
            new Dictionary<string, ProtocolCustomType>();

        public List<ProtocolEndpoint> Endpoints { get; } = new List<ProtocolEndpoint>();

        public List<ProtocolTool> Tools { get; } = new List<ProtocolTool>();

        public IProtocolType ResolveType(string type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.EndsWith("?"))
                return new ProtocolNullableType(ResolveType(type.Substring(0, type.Length - 1)));
            if (type.EndsWith("[]"))
                return new ProtocolArrayType(ResolveType(type.Substring(0, type.Length - 2)));
            if (ProtocolBuiltInType.BuiltInTypes.Contains(type))
                return new ProtocolBuiltInType(type);
            if (CustomTypes.TryGetValue(type, out var result))
                return result;
            throw new Exception($"Unknown type: {type}");
        }

        public IProtocolType ContextType { get; set; }
    }
}