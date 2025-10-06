using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace ABCRetailFunctions
{
    public class UploadToBlobFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public UploadToBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadToBlobFunction>();
            _blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        }

        [Function("UploadToBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "uploadblob")] HttpRequestData req)
        {
            var container = _blobServiceClient.GetBlobContainerClient("productimages");
            await container.CreateIfNotExistsAsync();

            if (req.Method.ToLower() == "post")
            {
                var fileName = req.Headers.TryGetValues("filename", out var vals)
                    ? vals.FirstOrDefault()
                    : $"file_{Guid.NewGuid()}.jpg";

                var blob = container.GetBlobClient(fileName);
                await blob.UploadAsync(req.Body, overwrite: true);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"File uploaded successfully. Blob URL: {blob.Uri}");
                return response;
            }
            else // GET request
            {
                var blobs = new List<string>();
                await foreach (var blobItem in container.GetBlobsAsync())
                    blobs.Add($"{container.Uri}/{blobItem.Name}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(string.Join("\n", blobs));
                return response;
            }
        }
    }
}
