using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CanvasAppPackager
{
    public class MsAppHelper
    {
        public static void CreateFromDirectory(string sourcePath, string outputName)
        {
            var files = new List<string>(Directory.GetFiles(sourcePath, "", SearchOption.AllDirectories));
            
            using (var zipToOpen = new FileStream(outputName, FileMode.Create))
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var name = Path.GetRelativePath(sourcePath, file);
                    ZipFileExtensions.CreateEntryFromFile(archive, file, name);
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
