using System;
using System.IO;
using AiApiGenerator.Protocol;
using Humanizer;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace AiApiGenerator.CSharp.Tools
{
    public class GenerateCodeTask : Task
    {
        [Required] public ITaskItem[] ProtocolFiles { get; set; }

        [Required] public string OutputDirectory { get; set; }
        // [Required] public string SettingNamespaceName { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, "Задача \"GenerateAiApiClientCode начата\"");
                foreach (var file in ProtocolFiles)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Генерация кода для файла протокола {file}");

                    var protocolParser = new ProtocolParser();
                    var generator = new CodeGenerator(protocolParser.ParseFile(file.ItemSpec));

                    var generatedCode = generator.GenerateCode("TestClient");

                    var outputPath = Path.Combine(
                        OutputDirectory,
                        Path.GetFileNameWithoutExtension(file.ItemSpec.Pascalize()) + "Client.generated.cs");

                    File.WriteAllText(outputPath, generatedCode);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
                return false;
            }
        }
    }
}