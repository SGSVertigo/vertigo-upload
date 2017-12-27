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
                TableQuery<File> query = new TableQuery<File>();
                query.FilterString = queryString;
                IEnumerable<File> files = tableBinding.ExecuteQuery(query);
                string jsonResponse = JsonConvert.SerializeObject(files);
                return req.CreateResponse(HttpStatusCode.OK, files, MediaTypeHeaderValue.Parse("application/json"));
            }catch (Exception e)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
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
