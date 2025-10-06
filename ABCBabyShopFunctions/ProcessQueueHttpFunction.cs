using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailFunctions
{
    public class ProcessQueueHttpFunction
    {
        private readonly ILogger _logger;
        private readonly ProcessQueueService _service;

        public ProcessQueueHttpFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessQueueHttpFunction>();

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
            _service = new ProcessQueueService(_logger, connectionString);
        }

        [Function("ProcessQueueHttp")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "processqueue")] HttpRequestData req)
        {
            _logger.LogInformation("HTTP trigger: Processing queue or displaying orders...");

            var results = await _service.ProcessQueueMessagesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);

            return response;
        }
    }
}
