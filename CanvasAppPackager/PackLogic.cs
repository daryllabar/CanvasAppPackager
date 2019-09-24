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
                Logger.Log("Copying app files for zip creation.");
                CopyFilesToZipFolder(appPath, zipPath, "MsApp", UnpackLogic.Paths.Code, UnpackLogic.Paths.Metadata);
                Logger.Log("Packaging Code files");
                foreach (var codeDirectory in Directory.GetDirectories(Path.Combine(appPath, UnpackLogic.Paths.Code)))
                {
                    var screen = PackScreen(codeDirectory);
                    var dir = Path.Combine(zipPath, UnpackLogic.Paths.Controls);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(Path.Combine(dir, screen.TopParent.ControlUniqueId) + ".json", screen.Serialize());
                }

                Logger.Log("Parsing AppInfo");
                var sourcePath = Path.Combine(appPath, UnpackLogic.Paths.Metadata);
                var metadataFiles = Directory.GetFiles(sourcePath); 
                var appInfo = AppInfo.Parse(File.ReadAllText(metadataFiles.Single(f => Path.GetExtension(f) == ".json")));
                var destinationPath = Path.Combine(mainAppPath, UnpackLogic.Paths.MsPowerApps, "apps", appInfo.AppId);
                MoveMetadataFilesFromExtract(sourcePath, destinationPath, metadataFiles);
                var msAppZipPath = Path.Combine(destinationPath, Path.GetFileName(appInfo.MsAppPath));
                Logger.Log($"Packing file {msAppZipPath}");
                MsAppHelper.CreateFromDirectory(zipPath, msAppZipPath);
            }
            finally
            {
                Directory.Delete(zipPath, true);
            }
        }

        public static string GetTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static CanvasAppScreen PackScreen(string codeDirectory)
        {
            var jsonFile = Path.Combine(codeDirectory, Path.GetFileName(codeDirectory)) + ".json";
            Logger.Log("Parsing file " + Path.GetFileNameWithoutExtension(jsonFile));
            var json = File.ReadAllText(jsonFile);
            var screen = JsonConvert.DeserializeObject<CanvasAppScreen>(json);
            Logger.Log("Packaging file " + screen.TopParent.ControlUniqueId + " from " + Path.GetFileNameWithoutExtension(jsonFile));
            PackChildControl(screen.TopParent, jsonFile);
            return screen;
        }

        private static void PackChildControl(IControl control, string jsonFile)
        {
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
                }
            }

            PackChildren(control, jsonFile);
        }

        private static void PackChildren(IControl control, string jsonFile)
        {
            var childrenByName = new Dictionary<string, Child>();
            foreach (var childDirectory in Directory.GetDirectories(Path.GetDirectoryName(jsonFile)))
            {
                var childJsonFile = Path.Combine(childDirectory, Path.GetFileName(childDirectory)) + ".json";
                var child = JsonConvert.DeserializeObject<Child>(File.ReadAllText(childJsonFile));
                PackChildControl(child, childJsonFile);
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

        private static void MoveMetadataFilesFromExtract(string sourcePath, string destinationPath, string[] metadataFiles)
        {
            Logger.Log($"Copying Metadata Files from \"{sourcePath}\" to \"{destinationPath}\".");
            Directory.CreateDirectory(destinationPath);
            foreach (var file in metadataFiles)
            {
                File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)), true);
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
