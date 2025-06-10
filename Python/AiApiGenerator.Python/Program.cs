// See https://aka.ms/new-console-template for more information

using AiApiGenerator.Protocol;
using AiApiGenerator.Python;

var parser = new ProtocolParser();
var protocol = parser.ParseFile(@"C:\Users\sergi\RiderProjects\AiApiGenerator\TestProtocols\AiDoc.yml");
var generator = new CodeGenerator(protocol);

foreach (var customType in protocol.CustomTypes.Values)
{
    Console.WriteLine(generator.GenerateModel(customType).WriteCode());
    Console.WriteLine();
}

foreach (var endpoint in protocol.Endpoints)
{
    Console.WriteLine(generator.GenerateRoute(endpoint).WriteCode());
    Console.WriteLine();
}