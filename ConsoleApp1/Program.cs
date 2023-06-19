using Binatrix.QNAP;
using System.ComponentModel;

string user = "admin";
string pass = "admin";
string baseFolder = "/Carpeta/subcarpeta";
string newFolder = "otra";
FileManager nas = new("http://192.168.1.10:8080");
try
{
    nas.Login(user, pass);
    if (nas.Exists(baseFolder, newFolder))
    {
        nas.Delete(baseFolder, newFolder);
    }
    nas.Create(baseFolder, newFolder);
    nas.Download(baseFolder, "1000008321.pdf", @"c:\temp\1.pdf");
    nas.Download(baseFolder, "2000008321.pdf", @"c:\temp\2.pdf");
    nas.Upload(@"c:\temp\1.pdf", $"{baseFolder}/{newFolder}");
    nas.Upload(@"c:\temp\2.pdf", $"{baseFolder}/{newFolder}");
    nas.Delete($"{baseFolder}/{newFolder}", new string[] { "1.pdf", "2.pdf" });
    var files = nas.List<FileResponse>(baseFolder, ListType.ALL, sort: ListSortBy.FILESIZE, dir: ListSortDirection.Descending);
    Console.WriteLine(string.Join<FileResponse>(",", files));
    var folders = nas.List<FileResponse>(baseFolder, ListType.FOLDER);
    Console.WriteLine(string.Join<FileResponse>(",", folders));
    var folders2 = nas.List<FolderResponse>(baseFolder, ListType.TREE);
    Console.WriteLine(string.Join<FileResponse>(",", folders));

    var stream = nas.Download(baseFolder, "test2.txt");
    using StreamReader reader = new(stream.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8);
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        Console.WriteLine(line);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
Console.WriteLine("FIN");
