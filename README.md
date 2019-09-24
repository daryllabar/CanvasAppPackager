# CanvasAppPackager
Tool used to Extract Canvas Apps, to be pushed into source control, and repackage it to be pushed to the cloud.  Basically a Power Apps version of the CRM SolutionPackager.  

# BETA WARNING!
This is extremely beta.  Currently the Pack functionality, although it creates a zip file that extracts to exactly the same files, fails when importing into Power Apps, so as such, is unusable.

# Unpack

 | Argument | Description |
 | --- | --- |
 | /action: {Pack\|Unpack} | Required. The action to perform.  The action can either be to extract the application package zip to a folder, or to pack a folder into a .zip file. |
 | /ZipFile: \<FilePath\> | Required. The path and name of an application package .zip file. When extracting, the file must exist and will be read from, or must be 'Latest' to use the latest downloaded file. When packing, the file is replaced." |
 | folder: \<FolderPath\> | Required. The path to a folder. When extracting, this folder is created and populated with component files. When packing, this folder must already exist and contain previously extracted component files. |
 | log: \<FilePath\> | Optional. A path and name to a log file. If the file already exists, new logging information is appended to the file. |
 
 Example: 
> CanvasAppPackager.exe /a:unpack /z:"C:\Downloads\PowerFlappy\PowerFlappy.zip" /f:"C:\TFS\PowerFlappy\Extract"
