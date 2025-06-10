using AiApiGenerator.Protocol.Models;
using Humanizer;

namespace AiApiGenerator.Python;

public class CodeGenerator
{
    private readonly Protocol.Models.Protocol _protocol;

    public CodeGenerator(Protocol.Models.Protocol protocol)
    {
        _protocol = protocol;
    }

    private static string ResolveType(IProtocolType protocolType)
    {
        return protocolType switch
        {
            ProtocolBuiltInType t => t.Name switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                "string" => "str",
                "uuid" => "UUID",
                "datetime" => "DateTime",
                "duration" => "TimeDelta",
                _ => throw new Exception($"Unknown protocol built-in type: {t.Name}"),
            },
            ProtocolCustomType t => t.Name,
            ProtocolNullableType t => $"Optional[{ResolveType(t.InnerType)}]",
            ProtocolArrayType t => $"list[{ResolveType(t.ArrayType)}]",
            _ => throw new Exception($"Unknown protocol type: {protocolType}")
        };
    }

    public CodeEntity GenerateModel(ProtocolCustomType model)
    {
        return new CodeEntity([
                new FromCodeImport("__future__", "annotations"),
                new FromCodeImport("typing", "Annotated"),
                new FromCodeImport("typing", "Optional"),
                new FromCodeImport("pydantic", "Field"),
                new FromCodeImport("pydantic", "BaseModel"),
            ], $"class {model.Name.Pascalize()}(BaseModel):\n" +
               $"{string.Join('\n', model.Fields
                   .Select(item => $"    {item.Key.Underscore()}: {ResolveType(item.Value)}"))}"
        );
    }
}