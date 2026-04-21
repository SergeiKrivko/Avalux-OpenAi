using System;
using System.Collections.Generic;
using System.IO;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text.Json;
using Avalux.OpenAi.Protocol.Models;

namespace Avalux.OpenAi.Client.Tools
{
    public class CodeGenerator
    {
        private readonly Avalux.OpenAi.Protocol.Models.Protocol _protocol;
        private readonly string _projectPath;

        public CodeGenerator(Avalux.OpenAi.Protocol.Models.Protocol protocol, string projectPath)
        {
            _protocol = protocol;
            _projectPath = projectPath;
        }

        public string GenerateCode(string codeNamespace)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(codeNamespace))
                .AddMembers(GenerateClientInterface(), GenerateClientAbstractClass());

            compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

            return "#nullable enable\n\n" + compilationUnit
                .NormalizeWhitespace()
                .ToFullString();
        }

        private string ClientInterfaceName => $"I{_protocol.Name.Pascalize()}Client";
        private string ClientInterfaceFullName => $"I{_protocol.Name.Pascalize()}Client<TContext>";
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
                    return SyntaxFactory.ParseTypeName(ClientInterfaceFullName + "." + customType.Name);

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
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddTypeParameterListParameters(SyntaxFactory.TypeParameter("TContext"));

            foreach (var protocolEndpoint in _protocol.Endpoints)
            {
                var method = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("global::System.Threading.Tasks.Task"))
                            .AddTypeArgumentListArguments(
                                SyntaxFactory.NullableType(ResolveType(protocolEndpoint.OutputType))),
                        SyntaxFactory.Identifier(protocolEndpoint.Name.Pascalize()))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .AddParameterListParameters(GenerateEndpointMethodParameters(protocolEndpoint))
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
                    .AddTypeParameterListParameters(
                        SyntaxFactory.TypeParameter("TContext")
                    )
                    .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause("TContext")
                        .AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)))
                    .AddBaseListTypes(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(ClientInterfaceFullName))
                    )
                    .AddMembers(
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(Uri apiUri, string apiKey, string model)\n" +
                            "{\n" +
                            "    _client = new global::Avalux.OpenAi.Client.OpenAiClient<TContext>(new global::Avalux.OpenAi.Client.Models.OpenAiClientOptions { ApiEndpoint = apiUri, ApiKey = apiKey, Model = model });\n" +
                            "    _Initialize();\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(global::Avalux.OpenAi.Client.OpenAiClient<TContext> client)\n" +
                            "{\n" +
                            "    _client = client;\n" +
                            "    _Initialize();\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private void _Initialize()\n" +
                            "{\n" +
                            $"    {string.Join("\n", _protocol.Tools.Select(tool => $"_client.AddFunction<{tool.Name.Pascalize()}Request, {ResolveType(tool.ResultType).ToFullString()}>(\"{tool.Name}\", {SymbolDisplay.FormatLiteral(tool.Description, true)}, _{tool.Name.Pascalize()}, new global::System.BinaryData({SymbolDisplay.FormatLiteral(JsonSerializer.Serialize(tool.ToTypeSchema()), true)}));"))}\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory
                                    .VariableDeclaration(
                                        SyntaxFactory.ParseTypeName(
                                            "global::Avalux.OpenAi.Client.OpenAiClient<TContext>"))
                                    .WithVariables(SyntaxFactory.SeparatedList(new[]
                                        { SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("_client")) })))
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                            )
                    )
                    .AddMembers(_protocol.Endpoints
                        .Select(endpoint =>
                            SyntaxFactory.MethodDeclaration(SyntaxFactory
                                        .GenericName(SyntaxFactory.Identifier("global::System.Threading.Tasks.Task"))
                                        .AddTypeArgumentListArguments(
                                            SyntaxFactory.NullableType(ResolveType(endpoint.OutputType))),
                                    endpoint.Name.Pascalize())
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                                )
                                .WithExplicitInterfaceSpecifier(
                                    SyntaxFactory.ExplicitInterfaceSpecifier(
                                        SyntaxFactory.IdentifierName(ClientInterfaceFullName)))
                                .AddParameterListParameters(GenerateEndpointMethodParameters(endpoint))
                                .WithBody(GetEndpointExpression(endpoint))
                        )
                        .Cast<MemberDeclarationSyntax>()
                        .ToArray()
                    )
                    .AddMembers(_protocol.Tools
                        .Select(GenerateToolRequestModelClass)
                        .Cast<MemberDeclarationSyntax>()
                        .ToArray())
                    .AddMembers(
                        _protocol.Tools
                            .Select(GenerateToolPrivateMethod)
                            .Cast<MemberDeclarationSyntax>()
                            .ToArray()
                    )
                    .AddMembers(
                        _protocol.Tools
                            .Select(GenerateToolProtectedMethod)
                            .Cast<MemberDeclarationSyntax>()
                            .ToArray()
                    );

            return classDeclaration;
        }

        private BlockSyntax GetEndpointExpression(ProtocolEndpoint endpoint)
        {
            var prompts = endpoint.Prompts.Any()
                ? endpoint.Prompts.Select(p =>
                    SymbolDisplay.FormatLiteral(endpoint.ProcessPrompt(File.ReadAllText(Path.Combine(_projectPath, p))),
                        true))
                : new[]
                {
                    SymbolDisplay.FormatLiteral(
                        endpoint.ProcessPrompt(File.ReadAllText(Path.Combine(_projectPath, "Prompts",
                            endpoint.Name.Pascalize() + ".prompt.txt"))), true)
                };

            var block = SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(
                    "var response = await _client.CompleteAsync(new global::Avalux.OpenAi.Client.Models.ChatRequest()" +
                    $"{string.Join("", prompts.Select(e => $".AddSystemPrompt({SymbolDisplay.FormatLiteral(e, true)})"))}" +
                    ".AddUserMessage(param)" +
                    (endpoint.Mode == ProtocolEndpointMode.Json
                        ? $".SetResponseType(\"{ResolveType(endpoint.OutputType).ToFullString()}\", new global::System.BinaryData({SymbolDisplay.FormatLiteral(JsonSerializer.Serialize(endpoint.OutputType.ToTypeSchema()), true)}))"
                        : "") + ", context);")
            );
            switch (endpoint.Mode)
            {
                case ProtocolEndpointMode.Text:
                    block = block.AddStatements(SyntaxFactory.ParseStatement("return response.ReadAsString();"));
                    break;
                case ProtocolEndpointMode.Json:
                    block = block.AddStatements(SyntaxFactory.ParseStatement(
                        $"return response.ReadAsJson<{ResolveType(endpoint.OutputType).ToFullString()}>();"));
                    break;
                case ProtocolEndpointMode.Code:
                    block = block.AddStatements(SyntaxFactory.ParseStatement("return response.ReadAsCode();"));
                    break;
            }

            return block;
        }

        private ParameterSyntax[] GenerateEndpointMethodParameters(ProtocolEndpoint endpoint)
        {
            var res = new List<ParameterSyntax>();
            res.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("param"))
                .WithType(ResolveType(endpoint.InputType)));
            res.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                .WithType(SyntaxFactory.ParseTypeName("TContext?"))
                .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("default"))));

            // res.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("options"))
            //     .WithType(SyntaxFactory.ParseTypeName("global::Avalux.OpenAi.Client.Models.RequestOptions?"))
            //     .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("null"))));
            return res.ToArray();
        }

        private MethodDeclarationSyntax GenerateToolPrivateMethod(ProtocolTool tool)
        {
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory
                        .GenericName(SyntaxFactory.Identifier("global::System.Threading.Tasks.Task"))
                        .AddTypeArgumentListArguments(ResolveType(tool.ResultType)),
                    "_" + tool.Name.Pascalize())
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                )
                .AddParameterListParameters(SyntaxFactory
                        .Parameter(SyntaxFactory.Identifier("param"))
                        .WithType(SyntaxFactory.ParseTypeName(tool.Name.Pascalize() + "Request")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                        .WithType(SyntaxFactory.ParseTypeName("TContext?"))
                )
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.ParseExpression(
                        $"{tool.Name.Pascalize()}({string.Join(", ", tool.Parameters.Select(p => $"param.{p.Name.Pascalize()}").Concat(new[] { "context" }))})")
                ))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            return method;
        }

        private MethodDeclarationSyntax GenerateToolProtectedMethod(ProtocolTool tool)
        {
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory
                        .GenericName(SyntaxFactory.Identifier("global::System.Threading.Tasks.Task"))
                        .AddTypeArgumentListArguments(ResolveType(tool.ResultType)),
                    tool.Name.Pascalize())
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                    SyntaxFactory.Token(SyntaxKind.AbstractKeyword)
                )
                .AddParameterListParameters(tool.Parameters.Select(parameter => SyntaxFactory
                        .Parameter(SyntaxFactory.Identifier(parameter.Name))
                        .WithType(ResolveType(parameter.Type))
                    ).ToArray()
                )
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            method = method.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                .WithType(SyntaxFactory.ParseTypeName("TContext?")));
            return method;
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

        private ClassDeclarationSyntax GenerateToolRequestModelClass(ProtocolTool tool)
        {
            return SyntaxFactory.ClassDeclaration(SyntaxFactory.Identifier(tool.Name.Pascalize() + "Request"))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                )
                .AddMembers(
                    tool.Parameters
                        .Select(item =>
                            SyntaxFactory.PropertyDeclaration(ResolveType(item.Type), item.Name.Pascalize())
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .AddAttributeLists(GenerateJsonPropertyNameAttribute(item.Name))
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
                    SyntaxFactory.ParseName("global::System.Text.Json.Serialization.JsonPropertyName"))
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(attributeArgument)));

            return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute));
        }

        public void GeneratePromptFiles(string projectPath)
        {
            Directory.CreateDirectory(Path.Combine(projectPath, "Prompts"));
            foreach (var endpoint in _protocol.Endpoints.Where(e => !e.Prompts.Any()))
            {
                var file = Path.Combine(projectPath, "Prompts", endpoint.Name.Pascalize() + ".prompt.txt");
                if (!File.Exists(file))
                    File.WriteAllText(file, string.Empty);
            }
        }
    }
}