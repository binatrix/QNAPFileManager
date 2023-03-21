using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;

namespace Binatrix.QNAP
{
    /// <summary>
    /// Class <c>FileManager</c> permite acceder al contenido del FileStation en la NAS.
    /// </summary>
    public class FileManager
    {
        private readonly string baseUrl;
        private readonly HttpClient client = new();
        private string? sid;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseUrl">URL base de acceso al servidor NAS</param>
        /// <example>FileManager nas = new("http://192.168.2.10:8080")</example>
        public FileManager(string baseUrl)
        {
            this.baseUrl = baseUrl;
            this.sid = null;
        }


        /// <summary>
        /// Acceso al servicio mediante credenciales
        /// </summary>
        /// <param name="user">Usuario</param>
        /// <param name="pass">Contraseña</param>
        /// <example>nas.Login("admin", "admin")</example>
        public void Login(string user, string pass)
        {
            sid = null;
            string encode_string = Convert.ToBase64String(Encoding.UTF8.GetBytes(pass));
            string url = $"{baseUrl}/cgi-bin/authLogin.cgi?user={user}&pwd={encode_string}";
            var response = client.GetAsync(url).Result;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.ReasonPhrase);
            string resp = response.Content.ReadAsStringAsync().Result;
            XmlDocument doc = new();
            doc.LoadXml(resp);
            XmlNode? root = doc.DocumentElement;
            XmlNode? node = root?.SelectSingleNode("authPassed");
            if (node?.FirstChild?.Value == "0")
                throw new Exception("Credenciales de acceso incorrectas");
            sid = root?.SelectSingleNode("authSid")?.FirstChild?.Value;
        }

        /// <summary>
        /// Descarga un archivo desde la NAS
        /// </summary>
        /// <param name="folder">Carpeta base en la NAS donde está el archivo</param>
        /// <param name="file">Nombre del archivo a descargar</param>
        /// <param name="destFile">Ruta completa del archivo de destino descargado</param>
        public void Download(string folder, string file, string destFile)
        {
            var url = BuildQuery("download", new NameValueCollection() {
                { "source_path", folder },
                { "source_file", file },
                { "isfolder", "0" },
                { "compress", "0" },
                { "source_total", "1" }
            });
            using var response = client.GetAsync(url).Result;
            if (response.Content.Headers.ContentType?.MediaType == "application/json") // Respuesta de status, hay un error
            {
                var linea = response.Content.ReadAsStringAsync().Result;
                CheckStatus(linea);
                return;
            }
            using var fileStream = new FileStream(destFile, FileMode.Create, FileAccess.Write);
            var stream = response.Content.ReadAsStreamAsync().Result;
            if (stream.Length == 0)
            {
                CheckStatus(JsonConvert.SerializeObject(new StatusResponse { Status = 5 }));
            }
            stream.CopyTo(fileStream);
        }

        /// <summary>
        /// Sube un archivo local hacia la NAS
        /// </summary>
        /// <param name="sourceFile">Ruta completa del archivo local a subir</param>
        /// <param name="destFolder">Ruta de la carpeta destino donde subir el archivo</param>
        public void Upload(string sourceFile, string destFolder)
        {
            string file = Path.GetFileName(sourceFile);
            var url = BuildQuery("upload", new NameValueCollection() {
                { "dest_path", destFolder },
                { "progress", (destFolder + "/" + file).Replace("/", "-") },
                { "type", "standard" },
                { "overwrite", "1" }
            });
            FileStream fileStream = new(sourceFile, FileMode.Open, FileAccess.Read);
            StreamContent streamContent = new(fileStream);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = file,
                FileName = sourceFile
            };
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(sourceFile, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            using var formData = new MultipartFormDataContent
            {
                { streamContent }
            };
            using var response = client.PostAsync(url, formData).Result;
            var res = response.Content.ReadAsStringAsync().Result;
            CheckStatus(res);
        }

        /// <summary>
        /// Crea una carpeta en la NAS
        /// </summary>
        /// <param name="parentFolder">Carpeta base en la NAS donde crear la nueva carpeta</param>
        /// <param name="newFolder">Nombre de la nueva carpeta a crear</param>
        public void Create(string parentFolder, string newFolder)
        {
            var url = BuildQuery("createdir", new NameValueCollection() {
                { "dest_path", parentFolder },
                { "dest_folder", newFolder }
            });
            ExecuteQuery(url);
        }

        /// <summary>
        /// Elimina una carpeta o un archivo desde la NAS
        /// </summary>
        /// <param name="parentFolder">Carpeta base en la NAS donde está el archivo a eliminar</param>
        /// <param name="name">Nombre del archivo o carpeta a eliminar</param>
        public void Delete(string parentFolder, string name)
        {
            Delete(parentFolder, new string[] { name });
        }

        /// <summary>
        /// Elimina varias carpetas o archivos desde la NAS
        /// </summary>
        /// <param name="parentFolder">Carpeta base en la NAS donde están los archivos a eliminar</param>
        /// <param name="names">Nombres de los archivos o carpetas a eliminar</param>
        public void Delete(string parentFolder, string[] names)
        {
            var nvc = new NameValueCollection() {
                { "path", parentFolder }
            };
            foreach (var name in names)
            {
                nvc.Add("file_name", name);
            }
            nvc.Add("file_total", names.Length.ToString());
            var url = BuildQuery("delete", nvc);
            ExecuteQuery(url);
        }

        /// <summary>
        /// indica si un archivo o carpeta existe en el servidor NAS
        /// </summary>
        /// <param name="parentFolder">Carpeta base en la NAS donde se buscará</param>
        /// <param name="name">Nombre del elemento a buscar</param>
        /// <returns>"true" si existe, "false" si no existe</returns>
        public bool Exists(string parentFolder, string name)
        {
            var url = BuildQuery("get_list", new NameValueCollection() {
                        { "path", parentFolder },
                        { "is_iso", "0" },
                        { "list_mode", "all" },
                        { "limit", "1" },
                        { "filename", name }
            });
            var response = client.GetAsync(url).Result;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.ReasonPhrase);
            var stream = response.Content.ReadAsStreamAsync().Result;
            string jsonString = new StreamReader(stream, Encoding.ASCII).ReadToEnd();
            if (jsonString.Contains("status"))
            {
                CheckStatus(jsonString);
                return false;
            }
            var items = JsonConvert.DeserializeObject<FilesResponse>(jsonString);
            if (items == null) return false;
            return items.Datas.FindAll(x => x.Filename.ToLower().Equals(name.ToLower())).Count > 0;
        }

        /// <summary>
        /// Lista el contenido de una carpeta en la NAS
        /// </summary>
        /// <param name="parentFolder">Carpeta base en la NAS donde se listará la información</param>
        /// <param name="type" cref="ListType">Tipo de listado</param>
        /// <param name="limit">Cantidad máxima de registros a retornar (por defecto 500)</param>
        /// <param name="sort" cref="ListSortBy">Campo para ordenar los resultados</param>
        /// <param name="dir" cref="ListSortDirection">Orden de los resultados (ascendentes o descendentes)</param>
        /// <typeparam name="T">Clase de retorno de resultados. Para TREE <see cref="FolderResponse"/>, para otros <see cref="FileResponse"/></typeparam>
        /// <example>
        /// <code>
        /// var files = nas.List&lt;FileResponse&gt;("/Public/imagenes", ListType.FILE, sort: ListSortBy.FILESIZE, dir: ListSortDirection.Descending);
        /// Console.WriteLine(string.Join&lt;FileResponse&gt;(",", files));
        /// var folders = nas.List&lt;FolderResponse&gt;("/Planvital/imagenes", ListType.TREE);
        /// Console.WriteLine(string.Join&lt;FileResponse&gt;(",", folders));
        /// </code>
        /// </example>
        /// <returns>Arreglo con el listado de resultados</returns>
        public T[] List<T>(string parentFolder, ListType type, int limit = 500, ListSortBy sort = ListSortBy.FILENAME, ListSortDirection dir = ListSortDirection.Ascending)
        {
            string url = "";
            switch (type)
            {
                case ListType.ALL:
                case ListType.FILE:
                case ListType.FOLDER:
                    url = BuildQuery("get_list", new NameValueCollection() {
                        { "path", parentFolder },
                        { "is_iso", "0" },
                        { "list_mode", "all" },
                        { "dir", dir == ListSortDirection.Ascending ? "ASC" : "DESC" },
                        { "limit", limit.ToString() },
                        { "sort", sort.ToString().ToLower() }
                    });
                    break;
                case ListType.TREE:
                    url = BuildQuery("get_tree", new NameValueCollection() {
                        { "node", parentFolder },
                        { "is_iso", "0" },
                    });
                    break;
            }
            var response = client.GetAsync(url).Result;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.ReasonPhrase);
            var stream = response.Content.ReadAsStreamAsync().Result;
            string jsonString = new StreamReader(stream, Encoding.ASCII).ReadToEnd();
            if (jsonString.Contains("status"))
            {
                CheckStatus(jsonString);
            }
            else
            {
                switch (type)
                {
                    case ListType.ALL:
                    case ListType.FILE:
                    case ListType.FOLDER:
                        {
                            var items = JsonConvert.DeserializeObject<FilesResponse>(jsonString);
                            if (items == null) return Array.Empty<T>();
                            else if (type == ListType.ALL) return (T[])(object)items.Datas.ToArray();
                            else if (type == ListType.FILE) return (T[])(object)items.Datas.FindAll(x => x.IsFolder == false).ToArray();
                            else if (type == ListType.FOLDER) return (T[])(object)items.Datas.FindAll(x => x.IsFolder == true).ToArray();
                        }
                        break;
                    case ListType.TREE:
                        {
                            var items = JsonConvert.DeserializeObject<List<T>>(jsonString);
                            return items == null ? Array.Empty<T>() : items.ToArray();
                        }
                }
            }
            return Array.Empty<T>();
        }

        #region Privado
        private string BuildQuery(string func, NameValueCollection nvc)
        {
            if (string.IsNullOrEmpty(sid))
                throw new Exception("No autenticado");
            var queryParams = new NameValueCollection
            {
                { "sid", sid },
                { "func", func },
                nvc
            };
            string q = ToQueryString(queryParams);
            string url = $"{baseUrl}/cgi-bin/filemanager/utilRequest.cgi?{q}";
            return url;
        }

        private void ExecuteQuery(string url)
        {
            var response = client.GetAsync(url).Result;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(response.ReasonPhrase);
            var stream = response.Content.ReadAsStreamAsync().Result;
            string jsonString = new StreamReader(stream, Encoding.ASCII).ReadToEnd();
            CheckStatus(jsonString);
        }

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (
                from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))
                ).ToArray();
            return string.Join("&", array);
        }

        private static void CheckStatus(string jsonString)
        {
            var json = JsonConvert.DeserializeObject<StatusResponse>(jsonString);
            int status = json == null ? 0 : json.Status;
            if (status != 1)
            {
                string err = status switch
                {
                    0 => "Failure",
                    2 => "File exists",
                    3 => "Not authorized",
                    4 => "Permission denied",
                    5 => "File doesn’t exist",
                    6 => "Compressing",
                    9 => "Quota limit exceeded",
                    25 => "Folder doesn’t exist",
                    33 => "Folder already exists",
                    _ => "Unknown",
                };
                throw new Exception($"Status {status}: {err}");
            }
        }

        #endregion
    }

    internal class StatusResponse
    {
        public int Status { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Clase con la información del modo TREE
    /// </summary>
    public class FolderResponse
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }

    internal class FilesResponse
    {
        public int Total { get; set; }
        public List<FileResponse> Datas { get; set; } = new List<FileResponse>();
    }

    /// <summary>
    /// Clase con la información de los modos ALL, FILE y FOLDER
    /// </summary>
    public class FileResponse
    {
        public string Filename { get; set; }
        public long Filesize { get; set; }
        public bool IsFolder { get; set; }
        public string Group { get; set; }
        public string Owner { get; set; }
        public DateTime MT { get; set; }
        public override string ToString()
        {
            return Filename;
        }
    }

    public enum ListType
    {
        ALL,
        FILE,
        FOLDER,
        TREE
    }

    public enum ListSortBy
    {
        FILENAME, FILESIZE, MT, OWNER, GROUP
    }
}