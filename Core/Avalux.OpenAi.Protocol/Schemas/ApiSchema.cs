using System;
using System.Collections.Generic;

namespace Avalux.OpenAi.Protocol.Schemas
{
    public class ApiSchema
    {
        public string Name { get; set; }
        public string Version { get; set; } = "1.0";
        public Dictionary<string, ApiEndpointSchema> Endpoints { get; set; } = new Dictionary<string, ApiEndpointSchema>();
        public Dictionary<string, ApiToolSchema> Tools { get; set; } = new Dictionary<string, ApiToolSchema>();
        public Dictionary<string, ApiSchemaFieldSchema[]> Schemas { get; set; } = new Dictionary<string, ApiSchemaFieldSchema[]>();
        public string Context { get; set; } = string.Empty;
    }
}