using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace uploader
{
    public static class vertigo_upload3r
    {
        [FunctionName("vertigo_upload3r")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req,
            [Blob("vertigostorage/uploaded/{rand-guid}")] CloudBlockBlob blob, 
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            if (req.Content.Headers.ContentLength > 100_000_000)
            {
                return req.CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, "Files should be below 100MB.");
            }
            blob.UploadFromStream(await req.Content.ReadAsStreamAsync());

            return req.CreateResponse(HttpStatusCode.OK, blob.Uri);
        }
    }
}
