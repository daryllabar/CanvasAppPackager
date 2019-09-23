static string SolutionsDirectory = @"C:\PathToExtractTo";
static string CanvasAppPackagerPath = @"C:\PathToCanvasAppPackager\CanvasAppPackager.exe";

void Main()
{
	var downloadFile = GetLatestZipFileInfoFromDownloads();
	//var releaseName = GetReleaseName(downloadFile);
	("Found Zip " + downloadFile).Dump();
	
	var solutionPath = CopyNewestDownloadedSolutionToTfsSolutionFolder(SolutionsDirectory, downloadFile);
	solutionPath.Dump();
	var extractPath = Path.Combine(Path.GetDirectoryName(solutionPath), "Extract");
	
	if(Directory.Exists(extractPath)){
		// Delete since if there are items that have been removed, they should be removed from the disk/Source Control
		try
		{
			Directory.Delete(extractPath, true);
		}
		catch (IOException ex)
		{
			
			if (ex.Message == "The directory is not empty."){
				"Delete Failed.  Retrying".Dump();
				// retry
				Thread.Sleep(1000);
				Directory.Delete(extractPath, true);
			} else{
				throw;
			}
		}
	}

	var args = string.Format(@"/a:unpack /z:""{0}"" /f:""{1}""",
											  solutionPath,
											  extractPath);
	
	(CanvasAppPackagerPath + " " + args).Dump();
	using (Process p = new Process())
	{
		p.StartInfo.Arguments = args;
		p.StartInfo.FileName = CanvasAppPackagerPath;
		p.StartInfo.CreateNoWindow = false;
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.RedirectStandardOutput = true;
		p.Start();
		
		StreamReader sr = p.StandardOutput;
		string output = sr.ReadToEnd();
		p.WaitForExit();
		output.Dump();
	}

	string.Empty.Dump();
	"Command to Pack:".Dump();
	string.Format(@"{2} /a:Pack /z:""{0}"" /f:""{1}""",
											  solutionPath,
											  extractPath, CanvasAppPackagerPath).Dump();
	//AddNewFiles(newPath, releaseName);
}



public static FileInfo GetLatestZipFileInfoFromDownloads()
{
	var directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
	var myFile = directory.GetFiles()
			 .OrderByDescending(f => f.LastWriteTime)
			 .First(f => f.Extension == ".zip");

	return myFile;
}

public static string GetNewPath(string directory, FileInfo solutionFile)
{
	return Path.Combine(directory, Path.GetFileName(SolutionsDirectory)) + ".zip";
}

private static string GetDefaultTfPath()
{
	var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
	if (programFiles == null)
	{
		throw new Exception($" Unable to get Environment Variable ProgramFiles(x86).");
	}
	var path = Path.Combine(programFiles, @"Microsoft Visual Studio 14.0\Common7\IDE\TF.exe");
	if (File.Exists(path))
	{
		return path;
	}
	else
	{
		// VS 2017 changed the location to be under the format of "Microsoft Visual Studio\(Year?Version?)\Edition"
		// attempt to future proof by checking all version and all editions
		path = Path.Combine(programFiles, @"Microsoft Visual Studio");
		if (!Directory.Exists(path))
		{
			return path;
		}
		foreach (var version in Directory.GetDirectories(path))
		{
			foreach (var edition in Directory.GetDirectories(version))
			{
				var tmp = Path.Combine(edition, @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe");
				if (File.Exists(tmp))
				{
					return tmp;
				}
			}
		}
		return path;
	}
}
public static string CopyNewestDownloadedSolutionToTfsSolutionFolder(string solutionsDirectory, FileInfo solutionFile)
{
	var newPath = GetNewPath(solutionsDirectory, solutionFile);
	newPath.Dump();

	if (!Directory.Exists(Path.GetDirectoryName(newPath)))
	{
		var path = Path.GetDirectoryName(newPath);
		("Created Directory " + path).Dump();
		Directory.CreateDirectory(Path.GetDirectoryName(newPath));
	}

	if (File.Exists(newPath))
	{
		("Deleted file: " + newPath).Dump();
		File.Delete(newPath);
	}

	solutionFile.CopyTo(newPath, true);

	return newPath;
}
