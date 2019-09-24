using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CanvasAppPackager.Poco;

namespace CanvasAppPackager
{
    class UnpackLogic
    {   
        public const string CodeFileExt = ".js";
        public const string EndOfRuleCode = "} // End of ";
        public struct Paths
        {
            public const string PackageApps = "apps";
            public const string Apps = "Apps";
            public const string Assets = "Assets";
            public const string Code = "Code";
            public const string Controls = "Controls";
            public const string Metadata= "MetadataFiles";
            public const string MsPowerApps = "Microsoft.PowerApps";
        }

        public static void Unpack(string file, string outputDirectory, Args.Args options)
        {
            if (options.Clobber && Directory.Exists(outputDirectory))
            {
                Logger.Log("Deleting files in " + outputDirectory);
                Directory.Delete(outputDirectory, true);
            }

            Logger.Log("Extracting files from " + file);
            ZipFile.ExtractToDirectory(file, outputDirectory, true);
            
            if (Path.GetExtension(file).ToLower() == ".zip")
            {
                ExtractApps(outputDirectory, options);
            }
            else
            {
                ExtractCanvasApp(outputDirectory);
            }
        }

        private static void ExtractApps(string outputDirectory, Args.Args options)
        {
            if (!Directory.Exists(Path.Combine(outputDirectory, Paths.MsPowerApps)))
            {
                Logger.Error($"Invalid zip file.  Missing root folder \"{Paths.MsPowerApps}\"");
                return;
            }

            var appsPath = Path.Combine(outputDirectory, Paths.MsPowerApps, Paths.PackageApps);
            foreach (var appSourcePath in Directory.GetDirectories(appsPath))
            {
                var appInfo = AppInfo.Parse(File.ReadAllText(Path.Join(appSourcePath, Path.GetFileName(appSourcePath)) + ".json"));
                var appOutputPath = Path.Combine(outputDirectory, Paths.Apps, appInfo.DisplayName);
                Logger.Log($"Extracting App {appInfo.DisplayName} - {appInfo.Description}");
                var msAppFilePath = Path.Combine(appsPath, appInfo.MsAppPath);
                Unpack(msAppFilePath, appOutputPath, options);
                MoveMetadataFiles(appOutputPath, msAppFilePath);
            }
        }

        private static void MoveMetadataFiles(string appOutputPath, string msAppFilePath)
        {
            var metadataFilesPath = Path.Combine(appOutputPath, Paths.Metadata);
            Directory.CreateDirectory(metadataFilesPath);
            Logger.Log($"Copying metadata files from {Path.GetDirectoryName(msAppFilePath)} to {metadataFilesPath}");
            File.Delete(msAppFilePath);
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(msAppFilePath)))
            {
                var destinationFile = Path.Combine(metadataFilesPath, Path.GetFileName(file));
                if (File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }
                File.Move(file, destinationFile);
            }
        }

        private static void ExtractCanvasApp(string appDirectory)
        {
            var codeDirectory = Path.Combine(appDirectory, Paths.Code);
            var controlsDir = Path.Combine(appDirectory, Paths.Controls);
            foreach (var file in Directory.GetFiles(controlsDir))
            {
                Logger.Log("Extracting file " + file);
                var json = File.ReadAllText(file);
                var screen = JsonConvert.DeserializeObject<CanvasAppScreen>(json);
                VerifySerialization(screen, json, file);
                var fileDirectory = Path.Combine(codeDirectory, screen.TopParent.Name);
                WriteRules(screen, screen.TopParent, fileDirectory);
            }
            Directory.Delete(controlsDir, true);
        }

        private static void VerifySerialization(CanvasAppScreen screen, string json, string file)
        {
            var newJson = screen.Serialize();
            if (json != newJson)
            {
                var jsonFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".original";
                // ReSharper disable once StringLiteralTypo
                var newJsonFile = Path.Combine(Path.GetDirectoryName(jsonFile), Path.GetFileNameWithoutExtension(jsonFile)) + ".reserialized";
                File.WriteAllText(jsonFile, json);
                File.WriteAllText(newJsonFile, newJson);
                var shortest = json.Length > newJson.Length
                    ? newJson
                    : json;

                var firstDifferentChar = shortest.Length;
                for (var i = 0; i < shortest.Length; i++)
                {
                    if (json[i] == newJson[i])
                    {
                        continue;
                    }

                    firstDifferentChar = i;
                    break;
                }

                throw new
                    Exception($"Unable to re-serialize json to match source!  Character at position {firstDifferentChar} is not correct.  To prevent potential app defects, extracting file {file} has stopped.{Environment.NewLine}See '{jsonFile}' for extracted version vs output version '{newJsonFile}'.");
            }
        }

        private static void WriteRules(CanvasAppScreen screen, IControl control, string directory)
        {
            Directory.CreateDirectory(directory);
            var sb = new StringBuilder();
            // Write out all Rules
            foreach (var rule in control.Rules)
            {
                sb.AppendLine(rule.Property + "(){"); 
                sb.AppendLine("\t" + rule.InvariantScript.Replace(Environment.NewLine, Environment.NewLine + "\t"));
                sb.AppendLine(EndOfRuleCode + rule.Property + Environment.NewLine);
                rule.InvariantScript = null;
            }
            File.WriteAllText(Path.Join(directory, control.Name) + CodeFileExt, sb.ToString());

            // Create List of Children so the order can be maintained
            var childrenOrder = new List<ChildOrder>(); 

            // Write out all Children Rules
            foreach (var child in control.Children)
            {
                WriteRules(screen, child, Path.Combine(directory, child.Name));
                childrenOrder.Add(new ChildOrder{Name = child.Name, ChildrenOrder = child.ChildrenOrder});
            }

            control.Children = null;
            control.ChildrenOrder = childrenOrder.Count == 0 ? null : childrenOrder;

            File.WriteAllText(Path.Combine(directory, Path.GetFileName(directory)) + ".json", 
                              screen.TopParent == control
                                  ? screen.Serialize(Formatting.Indented)
                                  : control.Serialize(Formatting.Indented));
        }
    }
}
