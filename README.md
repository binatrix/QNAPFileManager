# QNAPFileManager

Component that allows you to manage files from a NAS server by QNAP.

Available as a NuGet package

## Actions
Actions such as:
- Create folders
- Upload files
- Download files
- Delete files or folders
- List content

## Usage example
Instantiate object
```
FileManager nas = new("http://192.168.1.6:8080");
```
Login

```
nas.Login("user", "xxxxxxxx");
```
Check if a file or folder exists
```
if (nas.Exists("/Folder/images", "other"))
{
     ...
}
```
Create a folder
```
nas.Create("/Folder/images", "other");
```
Upload a file
```
nas.Upload(@"c:\temp\1.pdf", "/Folder/images/other");
```
Download a file
```
nas.Download("/Folder/images/other", "1.pdf", @"c:\temp\1.pdf");
```
Delete a file or folder
```
nas.Delete("/Folder/images/other", "1.pdf");
```
Delete multiple files or folders
```
nas.Delete("/Folder/images/other", new string[] { "1.pdf", "2.pdf" });
```
List all content (files and folders) sorted by criteria
```
var files = nas.List<FileResponse>("/Folder/images", ListType.ALL, sort: ListSortBy.FILESIZE, dir: ListSortDirection.Descending);
Console.WriteLine(string.Join<FileResponse>(",", files));
```
List files
```
var files = nas.List<FileResponse>("/Folder/images", ListType.FILE);
Console.WriteLine(string.Join<FileResponse>(",", files));
```
List folders
```
var folders = nas.List<FileResponse>("/Folder/images", ListType.FOLDER);
Console.WriteLine(string.Join<FileResponse>(",", folders));
```
List folder tree
```
var folders = nas.List<FolderResponse>("/Folder/images", ListType.TREE);
Console.WriteLine(string.Join<FolderResponse>(",", folders));
```