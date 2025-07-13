using System;
using System.Collections.Generic;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolToolParameter
    {
        public string Name { get; set; }
        public IProtocolType Type { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }

        private static string GetOpenApiType(IProtocolType type)
        {
            if (type is ProtocolNullableType nullableType)
                return GetOpenApiFormat(nullableType.InnerType);
            if (type is ProtocolCustomType)
                return "object";
            if (type is ProtocolBuiltInType builtInType)
            {
                switch (builtInType.Name)
                {
                    case "int":
                        return "int";
                    case "float":
                        return "float";
                    case "bool":
                        return "bool";
                    default:
                        return "string";
                }
            }

            throw new NotSupportedException();
        }

        private static string GetOpenApiFormat(IProtocolType type)
        {
            if (type is ProtocolBuiltInType builtInType)
            {
                switch (builtInType.Name)
                {
                    case "uuid":
                        return "uuid";
                    case "datetime":
                        return "datetime";
                    case "date":
                        return "date";
                    case "time":
                        return "time";
                    default:
                        return null;
                }
            }

            return null;
        }

        private static Dictionary<string, ApiToolParameter> GetInnerParameters(ProtocolCustomType type)
        {
            var result = new Dictionary<string, ApiToolParameter>();
            foreach (var field in type.Fields)
            {
                result.Add(field.Key, ToApiParameter(null, null, field.Value));
            }
            return result;
        }

        private static ApiToolParameter ToApiParameter(string name, string description, IProtocolType type)
        {
            return new ApiToolParameter
            {
                Name = name,
                Description = description,
                Type = GetOpenApiType(type),
                Required = !(type is ProtocolNullableType),
                Format = GetOpenApiFormat(type),
                Properties = type is ProtocolCustomType customType
                    ? GetInnerParameters(customType)
                    : new Dictionary<string, ApiToolParameter>(),
            };
        }

        public ApiToolParameter ToApiParameter()
        {
            return ToApiParameter(Name, Description, Type);
        }
    }
}