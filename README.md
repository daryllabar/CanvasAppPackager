# CanvasAppPackager
Tool used to Extract Canvas Apps, to be pushed into source control, and repackage it to be pushed to the cloud.  Basically a Power Apps version of the CRM SolutionPackager.  

# BETA WARNING!
This is extremely beta.  Would highly recommend immediatly attempting to unpack and pack you app, upload back to PowerApps, and then download and unpack it again to see if anything has changed prior to depending on this for your ALM.  The Pack feature is much more scary than the Unpack.

# Command Line Args

 | Argument | Short Arg | Description |
 | --- | --- | --- |
 | /action: {Pack\|Unpack} | /a | Required. The action to perform.  The action can either be to extract the application package zip to a folder, or to pack a folder into a .zip file. |
 | /clobber | /c | Optional. This argument is used only during an unpack action. Deletes all files in the output directory. |
 | /ZipFile: \<FilePath\> | /z | Required. The path and name of an application package .zip file. When extracting, the file must exist and will be read from, or must be 'Latest' to use the latest downloaded file. When packing, the file is replaced." |
 | /folder: \<FolderPath\> | /f | Required. The path to a folder. When extracting, this folder is created and populated with component files. When packing, this folder must already exist and contain previously extracted component files. |
 | /log: \<FilePath\> | /l | Optional. A path and name to a log file. If the file already exists, new logging information is appended to the file. |
 | NameOfApplication: \<AppName\> | /n | Optional.  The name of the application to extract the application as.  Helpful to remove postfixes from applications i.e. \"-Dev\" or \"-Hotfix\" |
 | /OnlyExtract | /o | Optional.  Only unpacks the MsApp file, does not parse it. |
 
 
Example Unpack: 
> CanvasAppPackager.exe /a:unpack /z:"C:\Downloads\PowerFlappy\PowerFlappy.zip" /f:"C:\TFS\PowerFlappy\Extract"

Example Pack:
> CanvasAppPackager.exe /a:pack /z:"C:\Downloads\PowerFlappy\PowerFlappy.zip" /f:"C:\TFS\PowerFlappy\Extract"
