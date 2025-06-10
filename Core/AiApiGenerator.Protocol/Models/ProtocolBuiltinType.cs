using System;
using System.Linq;
using System.Text.Json;

namespace AiApiGenerator.Protocol.Models
{
    public class ProtocolBuiltInType : IProtocolType
    {
        public static string[] BuiltInTypes { get; } =
        {
            "int",
            "bool",
            "float",
            "string",
            "uuid",
            "datetime",
            "date",
            "time",
            "duration",
        };

        public string Name { get; }

        public bool IsString => Name == "string";

        public ProtocolBuiltInType(string builtInType)
        {
            if (!BuiltInTypes.Contains(builtInType))
                throw new ArgumentException($"Unknown built-in type: {builtInType}");
            Name = builtInType;
        }

        public string JsonExample()
        {
            switch (Name)
            {
                case "int":
                    return "42";
                case "bool":
                    return "true";
                case "float":
                    return "3.14";
                case "string":
                    return JsonSerializer.Serialize(Name);
                case "uuid":
                    return JsonSerializer.Serialize(Guid.NewGuid());
                case "datetime":
                    return JsonSerializer.Serialize(DateTime.UtcNow);
                case "date":
                    return JsonSerializer.Serialize(DateTime.Today.ToShortDateString());
                case "time":
                    return JsonSerializer.Serialize(DateTime.UtcNow.ToShortTimeString());
                case "duration":
                    return JsonSerializer.Serialize(TimeSpan.FromMinutes(76.5));
                default:
                    throw new ArgumentException($"Unknown built-in type: {Name}");
            }
        }
    }
}