using System;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Avalux.OpenAi.Protocol.Models;

namespace Avalux.OpenAi.Client.Tools
{
    public class CodeGenerator
    {
        private readonly Avalux.OpenAi.Protocol.Models.Protocol _protocol;

        public CodeGenerator(Avalux.OpenAi.Protocol.Models.Protocol protocol)
        {
            _protocol = protocol;
        }

        public string GenerateCode(string codeNamespace)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Net.Http.Json")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text.Json.Serialization")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text.Json"))
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(codeNamespace))
                .AddMembers(GenerateClientInterface(), GenerateClientAbstractClass());

            compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

            return "#nullable enable\n\n" + compilationUnit
                .NormalizeWhitespace()
                .ToFullString();
        }

        private string ClientInterfaceName => $"I{_protocol.Name.Pascalize()}Client";
        private string ClientAbstractClassName => $"{_protocol.Name.Pascalize()}ClientBase";

        private TypeSyntax ResolveType(IProtocolType protocolType)
        {
            switch (protocolType)
            {
                case ProtocolBuiltInType builtInType:
                    switch (builtInType.Name)
                    {
                        case "int":
                            return SyntaxFactory.ParseTypeName("int");
                        case "float":
                            return SyntaxFactory.ParseTypeName("double");
                        case "bool":
                            return SyntaxFactory.ParseTypeName("bool");
                        case "string":
                            return SyntaxFactory.ParseTypeName("string");
                        case "datetime":
                            return SyntaxFactory.ParseTypeName("DateTime");
                        case "date":
                            return SyntaxFactory.ParseTypeName("DateOnly");
                        case "time":
                            return SyntaxFactory.ParseTypeName("TimeOnly");
                        case "uuid":
                            return SyntaxFactory.ParseTypeName("Guid");
                        case "duration":
                            return SyntaxFactory.ParseTypeName("TimeSpan");
                        default:
                            throw new InvalidOperationException($"{builtInType.Name} is not a built-in type.");
                    }

                case ProtocolCustomType customType:
                    return SyntaxFactory.ParseTypeName(ClientInterfaceName + "." + customType.Name);

                case ProtocolNullableType nullableType:
                    return SyntaxFactory.NullableType(ResolveType(nullableType.InnerType));

                case ProtocolArrayType arrayType:
                    return SyntaxFactory.ArrayType(ResolveType(arrayType.ArrayType)).WithRankSpecifiers(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.OmittedArraySizeExpression()))));

                default:
                    throw new InvalidOperationException($"Unknown protocol type: {protocolType}");
            }
        }

        private InterfaceDeclarationSyntax GenerateClientInterface()
        {
            var interfaceDeclaration =
                SyntaxFactory.InterfaceDeclaration(SyntaxFactory.Identifier(ClientInterfaceName))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            foreach (var protocolEndpoint in _protocol.Endpoints)
            {
                var method = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("Task"))
                            .AddTypeArgumentListArguments(
                                SyntaxFactory.NullableType(ResolveType(protocolEndpoint.OutputType))),
                        SyntaxFactory.Identifier(protocolEndpoint.Name.Pascalize()))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .AddParameterListParameters(SyntaxFactory
                        .Parameter(SyntaxFactory.Identifier("param"))
                        .WithType(ResolveType(protocolEndpoint.InputType))
                    )
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                interfaceDeclaration = interfaceDeclaration.AddMembers(method);
            }

            return interfaceDeclaration
                .AddMembers(_protocol.CustomTypes
                    .Select(item => GenerateModelClass(item.Value))
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray());
        }

        private ClassDeclarationSyntax GenerateClientAbstractClass()
        {
            var classDeclaration =
                SyntaxFactory.ClassDeclaration(SyntaxFactory.Identifier(ClientAbstractClassName))
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.AbstractKeyword)
                    )
                    .AddBaseListTypes(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(ClientInterfaceName))
                    )
                    .AddMembers(
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(Uri apiUri)\n" +
                            "{\n" +
                            "    _httpClient = new HttpClient { BaseAddress = apiUri };\n" +
                            $"    {string.Join("\n", _protocol.Tools.Select(tool => $"AddFunction<{ResolveType(tool.InputType).ToFullString()}, " + $"{ResolveType(tool.OutputType).ToFullString()}>(\"{tool.Name}\", {tool.Name.Pascalize()});"))}\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(HttpClient httpClient)\n" +
                            "{\n" +
                            "    _httpClient = httpClient;\n" +
                            $"    {string.Join("\n", _protocol.Tools.Select(tool => $"AddFunction<{ResolveType(tool.InputType).ToFullString()}, " + $"{ResolveType(tool.OutputType).ToFullString()}>(\"{tool.Name}\", {tool.Name.Pascalize()});"))}\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("HttpClient"))
                                    .WithVariables(SyntaxFactory.SeparatedList(new[]
                                        { SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("_httpClient")) })))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                            )
                    )
                    .AddMembers(_protocol.Endpoints
                        .Select(endpoint =>
                            SyntaxFactory.MethodDeclaration(SyntaxFactory.GenericName(SyntaxFactory.Identifier("Task"))
                                        .AddTypeArgumentListArguments(
                                            SyntaxFactory.NullableType(ResolveType(endpoint.OutputType))),
                                    endpoint.Name.Pascalize())
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                                )
                                .WithExplicitInterfaceSpecifier(
                                    SyntaxFactory.ExplicitInterfaceSpecifier(
                                        SyntaxFactory.IdentifierName(ClientInterfaceName)))
                                .AddParameterListParameters(SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier("param"))
                                    .WithType(ResolveType(endpoint.InputType))
                                )
                                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                    SyntaxFactory.ParseExpression(GetEndpointExpression(endpoint))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                        .Cast<MemberDeclarationSyntax>()
                        .ToArray()
                    )
                    .AddMembers(
                        _protocol.Tools
                            .Select(tool =>
                                SyntaxFactory.MethodDeclaration(SyntaxFactory
                                            .GenericName(SyntaxFactory.Identifier("Task"))
                                            .AddTypeArgumentListArguments(ResolveType(tool.OutputType)),
                                        tool.Name.Pascalize())
                                    .WithModifiers(SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                        SyntaxFactory.Token(SyntaxKind.AbstractKeyword)
                                    ))
                                    .AddParameterListParameters(SyntaxFactory
                                        .Parameter(SyntaxFactory.Identifier("param"))
                                        .WithType(SyntaxFactory.NullableType(ResolveType(tool.InputType)))
                                    )
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            )
                            .Cast<MemberDeclarationSyntax>()
                            .ToArray()
                    )
                    .AddMembers(
                        SyntaxFactory.ParseMemberDeclaration(
                            "private class AiMessage\n{\n    [JsonPropertyName(\"role\")] public string? Role { get; init; }\n\n    [JsonPropertyName(\"content\")] public string? Content { get; init; }\n\n    [JsonPropertyName(\"tool_call_id\")] public string? ToolCallId { get; init; }\n\n    [JsonPropertyName(\"tool_calls\")] public ToolCall[]? ToolCalls { get; init; }\n}") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private class AiRequestModel\n{\n    [JsonPropertyName(\"messages\")] public AiMessage[] Messages { get; set; } = [];\n}") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private class AiResponseModel\n{\n    [JsonPropertyName(\"messages\")] public AiMessage[] Messages { get; set; } = [];\n}") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private class FunctionCall\n{\n    [JsonPropertyName(\"name\")] public required string Name { get; init; }\n\n    [JsonPropertyName(\"arguments\")] public required string Arguments { get; init; }\n}") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private class ToolCall\n{\n    [JsonPropertyName(\"id\")] public required string Id { get; init; }\n\n    [JsonPropertyName(\"type\")] public string? Type { get; init; }\n\n    [JsonPropertyName(\"function\")] public required FunctionCall Function { get; init; }\n}") ??
                        throw new Exception("Internal error")
                    )
                    .AddMembers(
                        SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "MaxToolCalls")
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                SyntaxFactory.Token(SyntaxKind.VirtualKeyword)
                            ).AddAccessorListAccessors(
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(
                                        SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression("100")))
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            ),
                        SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "MaxRetries")
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                SyntaxFactory.Token(SyntaxKind.VirtualKeyword)
                            ).AddAccessorListAccessors(
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithExpressionBody(
                                        SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression("3")))
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            ),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private record Function(string Name, Func<string, Task<object?>> Func);") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration("private readonly List<Function> _functions = [];") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private async Task<string?> ProcessAsync<TIn>(string url, TIn request)\n{\n    var resp = await SendInitAsync(url, request);\n    for (int i = 0; i < MaxToolCalls; i++)\n    {\n        var retry = 0;\n        while (true)\n        {\n            try\n            {\n                var messages = resp.Messages.ToList();\n                var lastMessage = messages.Last();\n                if (lastMessage.ToolCalls == null || lastMessage.ToolCalls.Length == 0)\n                {\n                    Console.WriteLine($\"AI Result: {lastMessage.Content}\");\n                    if (lastMessage.Content == null)\n                        throw new Exception(\"Invalid response from AI\");\n                    return lastMessage.Content;\n                }\n\n                resp = await SendAsync(url, new AiRequestModel\n                {\n                    Messages = messages.Concat(await CallToolsAsync(lastMessage)).ToArray()\n                });\n                break;\n            }\n            catch (Exception)\n            {\n                retry++;\n                if (retry > MaxRetries)\n                    throw;\n                Console.WriteLine($\"Retry ({retry}/{MaxRetries})...\");\n            }\n        }\n    }\n\n    throw new Exception(\"Max calls reached\");\n}") ??
                        throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private async Task<AiResponseModel> SendAsync(string url, AiRequestModel request)\n{\n    var content = JsonContent.Create(request);\n    var resp = await _httpClient.PostAsync(url + \"/request\", content);\n    resp.EnsureSuccessStatusCode();\n    var result = await resp.Content.ReadFromJsonAsync<AiResponseModel>() ??\n                 throw new Exception(\"Failed to send request\");\n    return result;\n}") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private async Task<AiResponseModel> SendInitAsync<TIn>(string url, TIn request)\n{\n    var content = JsonContent.Create(request);\n    var resp = await _httpClient.PostAsync(url + \"/init\", content);\n    resp.EnsureSuccessStatusCode();\n    var result = await resp.Content.ReadFromJsonAsync<AiResponseModel>() ??\n                 throw new Exception(\"Failed to send request\");\n    return result;\n}") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private async Task<List<AiMessage>> CallToolsAsync(AiMessage message)\n    {\n        var messages = new List<AiMessage>();\n        if (message.ToolCalls == null)\n            return messages;\n        foreach (var toolCall in message.ToolCalls)\n        {\n            Console.WriteLine($\"Calling tool {toolCall.Function.Name}({toolCall.Function.Arguments})\");\n            var function = _functions.Single(f => f.Name == toolCall.Function.Name);\n            object? res;\n            try\n            {\n                res = await function.Func(toolCall.Function.Arguments);\n            }\n            catch (Exception e)\n            {\n                Console.WriteLine($\"Error in tool call: {toolCall.Function.Name}: {e.Message}\");\n                res = null;\n            }\n\n            messages.Add(new AiMessage\n            {\n                Role = \"tool\",\n                Content = JsonSerializer.Serialize(res),\n                ToolCallId = toolCall.Id,\n            });\n        }\n\n        return messages;\n    }") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private void AddFunction<TIn, TOut>(string name, Func<TIn?, Task<TOut>> func)\n{\n    _functions.Add(new Function(name, async data =>\n    {\n        var param = JsonSerializer.Deserialize<TIn>(data);\n        return await func(param);\n    }));\n}") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private string? ProcessString(string? content)\n{\n    return content;\n}") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private T? ProcessJson<T>(string? content)\n{\n    if (content == null)\n        return default;\n    if (content.Contains(\"```json\"))\n    {\n        content = content.Substring(\n            content.IndexOf(\"```json\", StringComparison.InvariantCulture) + \"```json\".Length);\n        content = content.Substring(0, content.IndexOf(\"```\", StringComparison.InvariantCulture));\n    }\n\n    return JsonSerializer.Deserialize<T>(content);\n}") ??
                        throw new Exception("Internal error")
                        ,
                        SyntaxFactory.ParseMemberDeclaration(
                            "private string? ProcessCode(string? content)\n{\n    if (content == null)\n        return null;\n    if (content.Contains(\"```\"))\n    {\n        content = content.Substring(\n            content.IndexOf(\"```\", StringComparison.InvariantCulture) + \"```\".Length);\n        content = content.Substring(0, content.IndexOf(\"```\", StringComparison.InvariantCulture));\n    }\n\n    return content;\n}") ??
                        throw new Exception("Internal error")
                    );

            return classDeclaration;
        }

        private string GetEndpointExpression(ProtocolEndpoint endpoint)
        {
            var typeParam = endpoint.Mode == ProtocolEndpointMode.Json
                ? $"<{ResolveType(endpoint.OutputType).ToFullString()}>"
                : "";
            return $"Process{endpoint.Mode}{typeParam}(await ProcessAsync(\"{endpoint.Name}\", param))";
        }

        private ClassDeclarationSyntax GenerateModelClass(ProtocolCustomType customType)
        {
            return SyntaxFactory.ClassDeclaration(SyntaxFactory.Identifier(customType.Name.Pascalize()))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                )
                .AddMembers(
                    customType.Fields
                        .Select(item =>
                            SyntaxFactory.PropertyDeclaration(ResolveType(item.Value), item.Key.Pascalize())
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddAttributeLists(GenerateJsonPropertyNameAttribute(item.Key))
                                .AddAccessorListAccessors(
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                    SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        )
                        .Cast<MemberDeclarationSyntax>()
                        .ToArray()
                );
        }

        private static AttributeListSyntax GenerateJsonPropertyNameAttribute(
            string jsonPropertyName)
        {
            // Создаем аргумент атрибута - строковый литерал
            var attributeArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(jsonPropertyName)));

            // Создаем сам атрибут с аргументом
            var attribute = SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("JsonPropertyName"))
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(attributeArgument)));

            return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute));
        }
    }
}