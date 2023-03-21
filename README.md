# QNAPFileManager

Componente que permite gestionar archivos desde un servidor NAS soportado por QNAP.

Disponible como paquete NuGet

## Acciones
Se permiten acciones tales como:
- Crear carpetas
- Subir archivos
- Descargar archivos
- Eliminar archivos o carpetas
- Listar contenido

## Ejemplo de uso

Crear objeto
```
FileManager nas = new("http://192.168.1.6:8080");
```
Login

```
nas.Login("usuario", "xxxxxxxx");
```
Verificar si un archivo o carpeta existe
```
if (nas.Exists("/Carpeta/imagenes", "otra"))
{
    ...
}
```
Crear una carpeta
```
nas.Create("/Carpeta/imagenes", "otra");
```
Subir un archivo
```
nas.Upload(@"c:\temp\1.pdf", "/Carpeta/imagenes/otra");
```
Descargar un archivo
```
nas.Download("/Carpeta/imagenes/otra", "1.pdf", @"c:\temp\1.pdf");
```
Eliminar un archivo o carpeta
```
nas.Delete("/Carpeta/imagenes/otra", "1.pdf");
```
Eliminar varios archivos o carpetas
```
nas.Delete("/Carpeta/imagenes/otra", new string[] { "1.pdf", "2.pdf" });
```
Listar todo el contenido (archivos y carpetas) ordenandos por criterios
```
var files = nas.List<FileResponse>("/Carpeta/imagenes", ListType.ALL, sort: ListSortBy.FILESIZE, dir: ListSortDirection.Descending);
Console.WriteLine(string.Join<FileResponse>(",", files));
```
Listar archivos
```
var files = nas.List<FileResponse>("/Carpeta/imagenes", ListType.FILE);
Console.WriteLine(string.Join<FileResponse>(",", files));
```
Listar carpetas
```
var folders = nas.List<FileResponse>("/Carpeta/imagenes", ListType.FOLDER);
Console.WriteLine(string.Join<FileResponse>(",", folders));
```
Listar estructura
```
var folders = nas.List<FolderResponse>("/Carpeta/imagenes", ListType.TREE);
Console.WriteLine(string.Join<FolderResponse>(",", folders));
```
