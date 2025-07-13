using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string _projectPath;

        public CodeGenerator(Avalux.OpenAi.Protocol.Models.Protocol protocol, string projectPath)
        {
            _protocol = protocol;
            _projectPath = projectPath;
        }

        public string GenerateCode(string codeNamespace)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Net.Http.Json")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text.Json.Serialization")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text.Json")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Reflection")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Avalux.OpenAi.Client")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Avalux.OpenAi.Client.Models"))
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
                    .AddBaseListTypes(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(ClientInterfaceName))
                    )
                    .AddMembers(
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(Uri apiUri, string? model = null)\n" +
                            "{\n" +
                            "    _client = new Avalux.OpenAi.Client.Client(new HttpClient { BaseAddress = apiUri }) { BaseModel = model ?? \"auto\" };\n" +
                            "    _Initialize();\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            $"protected {_protocol.Name.Pascalize()}ClientBase(HttpClient httpClient, string? model = null)\n" +
                            "{\n" +
                            "    _client = new Avalux.OpenAi.Client.Client(httpClient) { BaseModel = model ?? \"auto\" };\n" +
                            "    _Initialize();\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.ParseMemberDeclaration(
                            "private void _Initialize()\n" +
                            "{\n" +
                            $"    {string.Join("\n", _protocol.Tools.Select(tool => $"_client.AddFunction<{tool.Name.Pascalize()}Request, " + (_protocol.ContextType == null ? "" : ResolveType(_protocol.ContextType).ToFullString() + ", ") + $"{ResolveType(tool.ResultType).ToFullString()}>(\"{tool.Name}\", _{tool.Name.Pascalize()}, JsonSerializer.Deserialize<AiTool>({SymbolDisplay.FormatLiteral(tool.GenerateJsonDefinition(), true)})!);"))}\n" +
                            "}") ?? throw new Exception("Internal error"),
                        SyntaxFactory.FieldDeclaration(
                                SyntaxFactory
                                    .VariableDeclaration(SyntaxFactory.ParseTypeName("Avalux.OpenAi.Client.Client"))
                                    .WithVariables(SyntaxFactory.SeparatedList(new[]
                                        { SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("_client")) })))
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
                                .AddParameterListParameters(GenerateEndpointMethodParameters(endpoint))
                                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                    SyntaxFactory.ParseExpression(GetEndpointExpression(endpoint))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
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

        private string GetEndpointExpression(ProtocolEndpoint endpoint)
        {
            var typeParam = "<" + ResolveType(endpoint.InputType).ToFullString();
            if (endpoint.Mode == ProtocolEndpointMode.Json)
                typeParam += ", " + ResolveType(endpoint.OutputType);
            typeParam += ">";

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

            var res =
                $"await _client.Send{endpoint.Mode}RequestAsync{typeParam}([{string.Join(", ", prompts)}]";
            res += ", param";
            if (_protocol.ContextType != null)
                res += ", context";
            res += ", options)";
            return res;
        }

        private ParameterSyntax[] GenerateEndpointMethodParameters(ProtocolEndpoint endpoint)
        {
            var res = new List<ParameterSyntax>();
            res.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("param"))
                .WithType(ResolveType(endpoint.InputType)));
            if (_protocol.ContextType != null)
            {
                var param = SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                    .WithType(ResolveType(_protocol.ContextType));
                if (_protocol.ContextType is ProtocolNullableType)
                    param = param.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("null")));
                res.Add(param);
            }

            res.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("options"))
                .WithType(SyntaxFactory.ParseTypeName("RequestOptions?"))
                .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("null"))));
            return res.ToArray();
        }

        private MethodDeclarationSyntax GenerateToolPrivateMethod(ProtocolTool tool)
        {
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory
                        .GenericName(SyntaxFactory.Identifier("Task"))
                        .AddTypeArgumentListArguments(ResolveType(tool.ResultType)),
                    "_" + tool.Name.Pascalize())
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                )
                .AddParameterListParameters(SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier("param"))
                    .WithType(SyntaxFactory.ParseTypeName(tool.Name.Pascalize() + "Request"))
                )
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.ParseExpression(
                        $"{tool.Name.Pascalize()}({string.Join(", ", tool.Parameters.Select(p => $"param.{p.Name.Pascalize()}"))}" +
                        (
                            _protocol.ContextType == null
                                ? ""
                                : ", " + $"({ResolveType(_protocol.ContextType)})context"
                        ) + ")")
                ))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (_protocol.ContextType != null)
                method = method.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                    .WithType(ResolveType(_protocol.ContextType)));
            return method;
        }

        private MethodDeclarationSyntax GenerateToolProtectedMethod(ProtocolTool tool)
        {
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory
                        .GenericName(SyntaxFactory.Identifier("Task"))
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
            if (_protocol.ContextType != null)
                method = method.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                    .WithType(ResolveType(_protocol.ContextType)));
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
                    SyntaxFactory.ParseName("JsonPropertyName"))
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