namespace Avalux.OpenAi.Protocol.Schemas
{
    public class ApiSchemaFieldSchema
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }
}