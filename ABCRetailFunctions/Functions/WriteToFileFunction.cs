using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetailFunctions
{
    public class WriteToFileFunction
    {
        private readonly ILogger _logger;
        private readonly ShareServiceClient _fileServiceClient;

        public WriteToFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WriteToFileFunction>();
            _fileServiceClient = new ShareServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        }

        [Function("WriteToFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "writefile")] HttpRequestData req)
        {
            var share = _fileServiceClient.GetShareClient("ordershare");
            await share.CreateIfNotExistsAsync();
            var directory = share.GetRootDirectoryClient();

            if (req.Method.ToLower() == "post")
            {
                var fileName = req.Headers.TryGetValues("filename", out var vals)
                    ? vals.FirstOrDefault()
                    : $"log_{DateTime.UtcNow.Ticks}.txt";

                var fileClient = directory.GetFileClient(fileName);

                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                await fileClient.CreateAsync(memoryStream.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, memoryStream.Length), memoryStream);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"File {fileName} written successfully.");
                return response;
            }
            else // GET request
            {
                var fileList = new List<string>();
                await foreach (var item in directory.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                        fileList.Add(item.Name);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Files:\n{string.Join("\n", fileList)}");
                return response;
            }
        }
    }
}
