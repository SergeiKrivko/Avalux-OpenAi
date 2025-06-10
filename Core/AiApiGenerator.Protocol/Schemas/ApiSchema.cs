using System.Collections.Generic;

namespace AiApiGenerator.Protocol.Schemas
{
    public class ApiSchema
    {
        public string Name { get; set; }
        public Dictionary<string, ApiEndpointSchema> Endpoints { get; set; } = new Dictionary<string, ApiEndpointSchema>();
        public Dictionary<string, ApiToolSchema> Tools { get; set; } = new Dictionary<string, ApiToolSchema>();
        public Dictionary<string, ApiSchemaFieldSchema[]> Schemas { get; set; } = new Dictionary<string, ApiSchemaFieldSchema[]>();
    }
}