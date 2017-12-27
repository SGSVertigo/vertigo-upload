using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace uploader
{
    public static class vertigo_query
    {
        [FunctionName("vertigo_query")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req,
            [Table("Files", Connection = "AzureWebJobsStorage")]CloudTable tableBinding,
            TraceWriter log)
        {
            try
            {
                string queryString = TableQuery.GenerateFilterConditionForBool("Approved", QueryComparisons.Equal, true);
                TableQuery<TableFile> query = new TableQuery<TableFile>();
                query.FilterString = queryString;
                IEnumerable<TableFile> files = tableBinding.ExecuteQuery(query);
                IEnumerable<File> filteredFiles = files.Select(tf => new File(tf));
                HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK, filteredFiles, MediaTypeHeaderValue.Parse("application/json"));
                response.Headers.Add("Access-Control-Allow-Credentials", "true");
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                return response;
            }catch (Exception e)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        public class TableFile : TableEntity
        {
            public TableFile()
            {
                this.PartitionKey = "Data";
                this.Timestamp = DateTime.Now;
            }

            public string Name { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Video_Url { get; set; }
            public string File_Url { get; set; }
        }

        public class File
        {
            public File(TableFile file)
            {
                Name = file.Name;
                Title = file.Title;
                Description = file.Description;
                Video_Url = file.Video_Url;
                File_Url = file.File_Url;
            }

            public string Name { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Video_Url { get; set; }
            public string File_Url { get; set; }
        }
    }
}
