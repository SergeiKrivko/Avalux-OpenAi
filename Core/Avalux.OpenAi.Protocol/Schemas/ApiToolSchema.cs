using System;

namespace Avalux.OpenAi.Protocol.Schemas
{
    public class ApiToolSchema
    {
        public ApiSchemaFieldSchema[] Params { get; set; } = Array.Empty<ApiSchemaFieldSchema>();
        public string Result { get; set; }
        public string Description { get; set; }
        public string Example { get; set; } = string.Empty;
    }
}