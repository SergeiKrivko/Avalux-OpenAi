using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Avalux.OpenAi.Protocol.Models;

namespace Avalux.OpenAi.Client.Tools
{
    internal static class Helpers
    {
        public static TypeSchema ToTypeSchema(this ProtocolTool tool)
        {
            return new TypeSchema
            {
                Type = "object",
                Properties = tool.Parameters.ToDictionary(e => e.Name, e => e.Type.ToTypeSchema(e.Description)),
                RequiredProperties = tool.Parameters
                    .Where(e => !(e.Type is ProtocolNullableType))
                    .Select(e => e.Name)
                    .ToList(),
            };
        }

        public static TypeSchema ToTypeSchema(this IProtocolType type, string description = null)
        {
            while (type is ProtocolNullableType nullableType)
                type = nullableType.InnerType;
            var typeName = "object";
            string formatName = null;
            Dictionary<string, TypeSchema> properties = null;
            TypeSchema itemsType = null;
            List<string> required = null;
            List<string> enumOptions = null;

            switch (type)
            {
                case ProtocolBuiltInType builtInType:
                    switch (builtInType.Name)
                    {
                        case "int":
                        case "float":
                        case "bool":
                        case "string":
                            typeName = builtInType.Name;
                            break;
                        case "uuid":
                        case "datetime":
                        case "date":
                        case "time":
                            typeName = "string";
                            formatName = builtInType.Name;
                            break;
                    }

                    break;
                case ProtocolArrayType arrayType:
                    typeName = "array";
                    itemsType = arrayType.ArrayType.ToTypeSchema();
                    break;
                case ProtocolCustomType customType:
                    properties = customType.Fields
                        .ToDictionary(e => e.Key, e => e.Value.ToTypeSchema());
                    required = customType.Fields
                        .Where(e => !(e.Value is ProtocolNullableType))
                        .Select(e => e.Key)
                        .ToList();
                    break;
            }

            return new TypeSchema
            {
                Type = typeName,
                Format = formatName,
                Description = description,
                Properties = properties,
                ItemsType = itemsType,
                RequiredProperties = required,
                EnumOptions = enumOptions,
            };
        }
    }

    internal class TypeSchema
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Format { get; set; }

        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, TypeSchema> Properties { get; set; }

        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ICollection<string> RequiredProperties { get; set; }

        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ICollection<string> EnumOptions { get; set; }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TypeSchema ItemsType { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }
    }
}