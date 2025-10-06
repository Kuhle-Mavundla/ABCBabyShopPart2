using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;

namespace ABCRetailFunctions
{
    public class StoreToTableFunction
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tableServiceClient;

        public StoreToTableFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreToTableFunction>();
            _tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        }

        [Function("StoreToTable")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "storetotable")] HttpRequestData req)
        {
            var method = req.Method.ToLower();

            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();

            if (method == "post")
            {
                var json = await new StreamReader(req.Body).ReadToEndAsync();
                var entity = JsonSerializer.Deserialize<TableEntity>(json);

                if (entity == null)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid JSON payload.");
                    return bad;
                }

                entity.PartitionKey ??= "Orders";
                entity.RowKey ??= Guid.NewGuid().ToString();

                await tableClient.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Entity stored successfully with RowKey: {entity.RowKey}");
                return response;
            }
            else // GET request
            {
                var entities = tableClient.Query<TableEntity>().ToList();
                var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(json);
                return response;
            }
        }
    }
}
