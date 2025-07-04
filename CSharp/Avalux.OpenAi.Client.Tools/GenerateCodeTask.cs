﻿using System;
using System.IO;
using Avalux.OpenAi.Protocol;
using Humanizer;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Avalux.OpenAi.Client.Tools
{
    public class GenerateCodeTask : Task
    {
        [Required] public ITaskItem[] ProtocolFiles { get; set; }

        [Required] public string OutputDirectory { get; set; }
        [Required] public string ProjectDirectory { get; set; }
        [Required] public string RootNamespace { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, "Задача \"GenerateAiApiClientCode начата\"");
                foreach (var file in ProtocolFiles)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Генерация кода для файла протокола {file}");

                    var protocolParser = new ProtocolParser();
                    var protocol = protocolParser.ParseFile(file.ItemSpec);
                    var generator = new CodeGenerator(protocol, ProjectDirectory);

                    generator.GeneratePromptFiles(ProjectDirectory);

                    var generatedCode = generator.GenerateCode(RootNamespace);

                    var outputPath = Path.Combine(
                        OutputDirectory,
                        Path.GetFileNameWithoutExtension(file.ItemSpec.Pascalize()) +
                        $"{protocol.Name.Pascalize()}.generated.cs");

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