using System;

namespace Avalux.OpenAi.Protocol.Schemas
{
    public class ApiToolSchema
    {
        public ApiSchemaFieldSchema[] Params { get; set; }
        public string Result { get; set; }
        public string Description { get; set; }
        public string Example { get; set; } = string.Empty;
        public ApiToolAggregatorSchema Aggregate { get; set; }
    }

    public class ApiToolAggregatorSchema
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}