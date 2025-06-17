namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolToolParameter
    {
        public string Name { get; set; }
        public IProtocolType Type { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }

        private string GetOpenApiType()
        {
            if (Type is ProtocolBuiltInType builtInType)
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

            return "object";
        }

        private string GetOpenApiFormat()
        {
            if (Type is ProtocolBuiltInType builtInType)
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
                        return "";
                }
            }

            return "";
        }

        public ApiToolParameter ToApiParameter()
        {
            return new ApiToolParameter
            {
                Name = Name,
                Description = Description,
                Type = GetOpenApiType(),
                Required = !(Type is ProtocolNullableType),
                Format = GetOpenApiFormat(),
            };
        }
    }
}