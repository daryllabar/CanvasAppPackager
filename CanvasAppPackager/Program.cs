using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CanvasAppPackager
{
    class Program
    {
        static void Main(string[] args)
        {
            var testUnpack = true;
            var pack = new[]
            {
                "/a", "pack", 
                "/z", @"C:\Temp\PowerFlappy\PowerFlappyByDaryl.zip",
                "/f", @"C:\Temp\PowerFlappy\Extract"
            };

            var unpack = new[]
            {
                "/a", "unpack",
                "/z", @"C:\Temp\PowerFlappy\PowerFlappy.zip",
                "/f", @"C:\Temp\PowerFlappy\Extract",
                "/n", @"PowerFlappy"
                //,"/r", "Si_3|Wm"
            };
            var options = Args.Args.Parse(args);

            if (Debugger.IsAttached)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                options = Args.Args.Parse(testUnpack ? unpack: pack);
            }

            if (!string.IsNullOrWhiteSpace(options.LogPath))
            {
                Logger.Error("Log Path not implemented!");
            }

            try
            {
                switch (options.Action)
                {
                    case Args.Args.ActionType.Pack:
                        Pack(options);
                        break;
                    case Args.Args.ActionType.Unpack:
                        Unpack(options);
                        break;
                    default:
                        Logger.Error("Invalid action argument: " + options.ActionText + ". Valid values: pack/unpack.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            Console.WriteLine("Finished!");
            //Console.ReadLine();
        }

        private static void Pack(Args.Args options)
        {
            if (!Directory.Exists(options.UnpackPath))
            {
                Logger.Error($"Directory at: \"{options.UnpackPath}\" does not exist!");
                return;
            }
            PackLogic.Pack(options);
        }

        private static void Unpack(Args.Args options)
        {
            if (string.Equals(options.PackageZip, "latest", StringComparison.OrdinalIgnoreCase))
            {
                options.PackageZip = GetLatestZipFromDownloads().FullName;
            }

            if (!File.Exists(options.PackageZip))
            {
                Logger.Error($"File at: \"{options.PackageZip}\" does not exist!");
                return;
            }

            UnpackLogic.Unpack(options.PackageZip, options.UnpackPath, options);
        }

        public static FileInfo GetLatestZipFromDownloads()
        {
            var directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            var myFile = directory.GetFiles()
                                  .OrderByDescending(f => f.LastWriteTime)
                                  .First(f => f.Extension == ".zip");

            return myFile;
        }
    }
}
