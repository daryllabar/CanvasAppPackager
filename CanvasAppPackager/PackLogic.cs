using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CanvasAppPackager.Poco;
using Newtonsoft.Json;

namespace CanvasAppPackager
{
    class PackLogic
    {
        public static void Pack(Args.Args options)
        {
            if (File.Exists(Path.Combine(options.UnpackPath, UnpackLogic.Paths.ManifestFileName)))
            {
                PackZip(options);
            }
            else if (File.Exists(Path.Combine(options.UnpackPath, UnpackLogic.Paths.Header)))
            {
                PackApp(options.UnpackPath, options.PackageZip);
            }
            else
            {
                throw new Exception("There is no manifest.json or Header.json found in the extract folder");
            }

        }

        private static void PackZip(Args.Args options)
        {
            var zipPath = GetTemporaryDirectory();
            try
            {
                CopyFilesToZipFolder(options.UnpackPath, zipPath, "non app source files", UnpackLogic.Paths.Apps);
                foreach (var appPath in Directory.GetDirectories(Path.Combine(options.UnpackPath, UnpackLogic.Paths.Apps)))
                {
                    PackApp(appPath, zipPath);
                }

                Logger.Log("Creating zip file " + options.PackageZip);
                if (File.Exists(options.PackageZip))
                {
                    File.Delete(options.PackageZip);
                }

                ZipFile.CreateFromDirectory(zipPath, options.PackageZip);
            }
            finally
            {
                Directory.Delete(zipPath, true);
            }
        }

        private static void PackApp(string appPath, string mainAppPath)
        {
            Logger.Log("Processing App at: " + appPath);
            var zipPath = GetTemporaryDirectory();
            try
            {
                Logger.Log("Parsing Auto Values");
                var extractor = AutoValueExtractor.Parse(Path.Combine(appPath, UnpackLogic.Paths.Code, UnpackLogic.Paths.AutoValues) + UnpackLogic.DataFileExt);

                Logger.Log("Copying app files for zip creation.");
                CopyFilesToZipFolder(appPath, zipPath, "MsApp", UnpackLogic.Paths.Code, UnpackLogic.Paths.Metadata);
                
                Logger.Log("Packaging Code files");
                PackScreens(appPath, zipPath, extractor);
                
                Logger.Log("Parsing AppInfo");
                var sourcePath = Path.Combine(appPath, UnpackLogic.Paths.Metadata);
                string msAppZipPath;

                if (Directory.Exists(sourcePath))
                {
                    var metadataFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories); 
                    var appInfo = AppInfo.Parse(File.ReadAllText(metadataFiles.Single(f => Path.GetExtension(f) == ".json")));
                    var destinationPath = Path.Combine(mainAppPath, UnpackLogic.Paths.MsPowerApps, "apps", appInfo.AppId);
                    MoveMetadataFilesFromExtract(appInfo, sourcePath, destinationPath, metadataFiles);
                    msAppZipPath = Path.Combine(destinationPath, Path.GetFileName(appInfo.MsAppPath));
                }
                else
                {
                    msAppZipPath = mainAppPath;
                }

                Logger.Log("Parsing Resource\\PublisherInfo");
                RestoreAutoNamedFiles(zipPath);

                Logger.Log($"Packing file {msAppZipPath}");
                MsAppHelper.CreateFromDirectory(zipPath, msAppZipPath);
            }
            finally
            {
                Directory.Delete(zipPath, true);
            }
        }

        private static void PackScreens(string appPath, string zipPath, AutoValueExtractor extractor)
        {
            foreach (var codeDirectory in Directory.GetDirectories(Path.Combine(appPath, UnpackLogic.Paths.Code)))
            {
                var screen = PackScreen(codeDirectory, extractor);
                var dir = Path.Combine(zipPath, UnpackLogic.Paths.Controls);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(Path.Combine(dir, screen.TopParent.ControlUniqueId) + ".json", screen.Serialize(Formatting.Indented));
            }
        }

        public static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static CanvasAppScreen PackScreen(string codeDirectory, AutoValueExtractor extractor)
        {
            var jsonFile = Path.Combine(codeDirectory, Path.GetFileName(codeDirectory)) + ".json";
            Logger.Log("Parsing file " + Path.GetFileNameWithoutExtension(jsonFile));
            var json = File.ReadAllText(jsonFile);
            var screen = JsonConvert.DeserializeObject<CanvasAppScreen>(json);
            Logger.Log("Packaging file " + screen.TopParent.Name + " from " + Path.GetFileNameWithoutExtension(jsonFile));
            PackChildControl(screen.TopParent, jsonFile, extractor);
            return screen;
        }

        private static void PackChildControl(IControl control, string jsonFile, AutoValueExtractor extractor)
        {
            extractor.Inject(control, jsonFile);
            if (control.Template?.ComponentDefinitionInfo?.Children != null)
            {
                extractor.InjectComponentChildren(control.Template.ComponentDefinitionInfo.Children, jsonFile);
            }
            Logger.Log("Packing Control " + control.Name);
            var code = File.ReadAllText(Path.Combine(Path.GetDirectoryName(jsonFile), Path.GetFileNameWithoutExtension(jsonFile)) + UnpackLogic.CodeFileExt);
            var propertiesByName = new Dictionary<string, string>();
            var ruleCodes = code.Replace(Environment.NewLine + "\t", Environment.NewLine)
                                .Split(Environment.NewLine + UnpackLogic.EndOfRuleCode);
            var endOfNameLine = "(){" + Environment.NewLine;
            foreach (var ruleCode in ruleCodes)
            {
                var index = ruleCode.IndexOf(endOfNameLine, StringComparison.Ordinal);
                if (index == -1)
                {
                    break;
                }

                var name = ruleCode.Substring(0, index).Split(Environment.NewLine).Last();
                propertiesByName.Add(name, ruleCode.Substring(index + endOfNameLine.Length));
            }

            foreach (var property in control.Rules)
            {
                if (propertiesByName.TryGetValue(property.Property, out var value))
                {
                    property.InvariantScript = value;
                    extractor.Inject(property, jsonFile);
                }
            }

            PackChildren(control, jsonFile, extractor);
        }

        private static void PackChildren(IControl control, string jsonFile, AutoValueExtractor extractor)
        {
            var childrenByName = new Dictionary<string, Child>();
            foreach (var childDirectory in Directory.GetDirectories(Path.GetDirectoryName(jsonFile)))
            {
                var childJsonFile = Path.Combine(childDirectory, Path.GetFileName(childDirectory)) + ".json";
                var child = JsonConvert.DeserializeObject<Child>(File.ReadAllText(childJsonFile));
                PackChildControl(child, childJsonFile, extractor);
                childrenByName.Add(child.Name, child);
            }

            if (childrenByName.Count == 0)
            {
                if (control.ChildrenOrder?.Count > 0)
                {
                    throw new Exception($"Unable to find child control \"{control.ChildrenOrder.First().Name}\" specified in children order for \"{control.Name}\".");
                }

                return;
            }

            var newChildList = new List<Child>();
            foreach (var child in control.ChildrenOrder)
            {
                if (childrenByName.TryGetValue(child.Name, out var fullChild))
                {
                    newChildList.Add(fullChild);
                    childrenByName.Remove(child.Name);
                }
                else
                {
                    throw new Exception($"Unable to find child control \"{child.Name}\" specified in children order for \"{control.Name}\".");
                }
            }

            if (childrenByName.Count > 0)
            {
                throw new Exception($"No ChildrenOrder specified for \"{childrenByName.First().Key}\" in \"{control.Name}\".");
            }

            control.Children = newChildList;
            control.ChildrenOrder = null;
        }

        private static void CopyFilesToZipFolder(string appPath, string zipPath, string description, params string[] directoriesToSkip)
        {
            Logger.Log($"Copying {description} files for zip creation");
            var hash = new HashSet<string>();
            foreach(var dir in directoriesToSkip)
            {
                hash.Add(Path.Combine(appPath, dir).ToLower());
            }

            foreach (var directory in Directory.GetDirectories(appPath)
                                               .Where(p => !hash.Contains(p.ToLower())))
            {
                CopyDirectory(directory, Path.Combine(zipPath, Path.GetFileName(directory)));
            }

            foreach (var file in Directory.GetFiles(appPath))
            {
                File.Copy(file, Path.Combine(zipPath, Path.GetFileName(file)));
            }
        }

        private static void MoveMetadataFilesFromExtract(AppInfo appInfo, string sourcePath, string destinationPath, string[] metadataFiles)
        {
            Logger.Log($"Copying Metadata Files from \"{sourcePath}\" to \"{destinationPath}\".");
            Directory.CreateDirectory(destinationPath);
            foreach (var file in metadataFiles)
            {
                var fileName = Path.Combine(destinationPath, Path.GetFileName(file));
                if (Path.GetFileName(file) == UnpackLogic.Paths.BackgroundImage)
                {
                    fileName = Path.Combine(destinationPath, Path.GetFileName(appInfo.BackgroundImage));
                }
                else if (Path.GetFileName(Path.GetDirectoryName(file)) == UnpackLogic.Paths.Icons
                                                                   && appInfo.Icons.TryGetValue(Path.GetFileNameWithoutExtension(file) + "Uri", out var map))
                {
                    fileName = Path.Combine(Path.GetDirectoryName(destinationPath), map);
                }
                File.Copy(file, fileName, true);
                RemoveJsonFormatting(fileName);
            }
        }

        private static void RemoveJsonFormatting(string destinationFile)
        {
            if (Path.GetExtension(destinationFile)?.ToLower() != ".json")
            {
                return;
            }

            var fileLines = File.ReadAllLines(destinationFile);
            if (fileLines.Length >= 1 && fileLines[0].StartsWith(UnpackLogic.UnformattedPrefix))
            {
                File.WriteAllText(destinationFile, fileLines[0].Substring(UnpackLogic.UnformattedPrefix.Length));
            }
        }


        private static void RestoreAutoNamedFiles(string appDirectory)
        {
            var resourceFilesPath = Path.Combine(appDirectory, UnpackLogic.Paths.Resources);
            var publishInfo = Path.Combine(resourceFilesPath, UnpackLogic.Paths.ResourcePublishFileName);
            Logger.Log("Extracting file " + publishInfo);
            var json = File.ReadAllText(publishInfo);
            var info = JsonConvert.DeserializeObject<PublishInfo>(json);

            // Copy Logo. Optional file. 
            var fromName = Path.Combine(resourceFilesPath, UnpackLogic.Paths.LogoImage + Path.GetExtension(info.LogoFileName));
            if (File.Exists(fromName))
            {
                var toName = Path.Combine(appDirectory, UnpackLogic.Paths.Resources, info.LogoFileName);
                Logger.Log($"Restoring auto named file '{fromName}' to '{toName}'.");
                File.Move(fromName, toName);
            }

            // Rename Component Files
            var componentsPath = Path.Combine(appDirectory, UnpackLogic.Paths.Components);
            if (Directory.Exists(componentsPath))
            {
                RestoreRenamedComponetFiles(componentsPath);
            }
        }

        private static void RestoreRenamedComponetFiles(string componentsPath)
        {
            foreach (var file in Directory.GetFiles(componentsPath))
            {
                Logger.Log("Extracting file " + file);
                var component = JsonConvert.DeserializeObject<CanvasAppScreen>(File.ReadAllText(file));
                var toName = Path.Combine(componentsPath, component.TopParent.ControlUniqueId + Path.GetExtension(file));
                Logger.Log($"Renaming component file '{file}' to '{toName}'.");
                File.Delete(toName);
                File.Move(file, toName);
            }
        }

        private static void CopyDirectory(string sourceDirName, string destDirName)
        {
            var filesToBeCopied = Directory.GetFiles(sourceDirName, string.Empty, SearchOption.AllDirectories);

            Parallel.ForEach(filesToBeCopied, source =>
            {
                var path = Path.GetRelativePath(sourceDirName, source);
                var destination = Path.Combine(destDirName, path);
                if (!Directory.Exists(Path.GetDirectoryName(destination)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                }
                File.Copy(source, destination, true);
            });
        }
    }
}
