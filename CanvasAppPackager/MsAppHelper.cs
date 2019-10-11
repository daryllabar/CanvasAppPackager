using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CanvasAppPackager
{
    public class MsAppHelper
    {
        private const string ParentZipEntryFolder = "ExternalComponent";

        public static void CreateFromDirectory(string sourcePath, string outputName)
        {
            var files = new List<string>(Directory.GetFiles(sourcePath, "", SearchOption.AllDirectories));
            
            using (var zipToOpen = new FileStream(outputName, FileMode.Create))
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var name = Path.GetRelativePath(sourcePath, file);
                    if (GetTopDirectory(name).Equals(ParentZipEntryFolder, StringComparison.InvariantCultureIgnoreCase))
                    {
                        name = name.Substring(ParentZipEntryFolder.Length, name.Length - ParentZipEntryFolder.Length);
                    }
                    ZipFileExtensions.CreateEntryFromFile(archive, file, name);
                }
            }
        }

        private static string GetTopDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            return path.Split(new char[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwrite)
        {
            using (var zip = new ZipArchive(File.OpenRead(sourceArchiveFileName)))
            {
                foreach (var entry in zip.Entries)
                {
                    var outputName = Path.Combine(destinationDirectoryName, (entry.FullName.StartsWith("\\")
                                                                                ? ParentZipEntryFolder
                                                                                : string.Empty
                                                                            ) + entry.FullName);
                    if (!Directory.Exists(Path.GetDirectoryName(outputName)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputName));
                    }
                    entry.ExtractToFile(outputName, overwrite);

                }
            }
        }

        public static void AddFiles(Dictionary<string, List<string>> filesByRootPath, string outputName)
        {
            using (var zipToOpen = new FileStream(outputName, FileMode.Create))
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (var files in filesByRootPath)
                {
                    foreach (var file in files.Value)
                    {
                        var name = Path.GetRelativePath(files.Key, file);
                        ZipFileExtensions.CreateEntryFromFile(archive, file, name);
                    }
                }
            }
        }
    }
}
