using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace uploader
{
    public static class vertigo_upload3r
    {
        [FunctionName("vertigo_upload3r")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req,
            [Blob("vertigostorage/uploaded/{rand-guid}")] CloudBlockBlob blob,
            [Table("Files", Connection = "MyStorageConnectionAppSetting")]CloudTable tableBinding,
            TraceWriter log)
        {
            try
            {
                log.Info("C# HTTP trigger function processed a request.");
                if (req.Content.Headers.ContentLength > 100_000_000)
                {
                    return req.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, "Files should be below 100MB.");
                }
                MemoryStream ms = new MemoryStream();
                await req.Content.CopyToAsync(ms);
                await blob.UploadFromStreamAsync(ms);
                ms.Position = 0;
                File file = new File();

                await ParseFiles(ms, req.Content.Headers.ContentType, (string s, Stream st) => blob.UploadFromStream(st), file);
                file.File_Url = blob.Uri.ToString();
                file.RowKey = blob.Name.ToAzureKeyString();
                TableOperation updateOperation = TableOperation.Insert(file);
                TableResult result = tableBinding.Execute(updateOperation);
                return req.CreateResponse(HttpStatusCode.OK, blob.Uri);
            }catch (Exception e)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        public static async Task ParseFiles(Stream data, MediaTypeHeaderValue contentType, Action<string, Stream> fileProcessor, File file)
        {
            var streamContent = new StreamContent(data);
            streamContent.Headers.ContentType = contentType;

            var provider = await streamContent.ReadAsMultipartAsync();

            foreach (var httpContent in provider.Contents)
            {
                var fileName = httpContent.Headers.ContentDisposition.FileName;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    using (Stream fileContents = await httpContent.ReadAsStreamAsync())
                    {
                        fileProcessor(fileName, fileContents);
                    }
                } else
                {
                    string content = await httpContent.ReadAsStringAsync();
                    switch (httpContent.Headers.ContentDisposition.Name.Trim('\"'))
                    {
                        case "fields[name]":
                            file.Name = content;
                            break;
                        case "fields[email]":
                            file.Email = content;
                            break;
                        case "fields[title]":
                            file.Title = content;
                            break;
                        case "fields[description]":
                            file.Description = content;
                            break;
                        case "fields[video_url]":
                            file.Video_Url = content;
                            break;
                        default:
                            break;
                    }
                        
                }

                
            }
        }

        public static string ToAzureKeyString(this string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str
                .Where(c => c != '/'
                            && c != '\\'
                            && c != '#'
                            && c != '/'
                            && c != '?'
                            && !char.IsControl(c)))
                sb.Append(c);
            return sb.ToString();
        }


        public class File : TableEntity
        {
            public File()
            {
                this.PartitionKey = "Data";
                this.Timestamp = DateTime.Now;
            }

            public string Name { get; set; }
            public string Email { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Video_Url { get; set; }
            public string File_Url { get; set; }
        }
    }
}
