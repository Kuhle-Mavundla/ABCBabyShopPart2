using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

namespace ABCBabyShop_3.Functions
{
    public class BabyShopFunctions
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _blobContainerName;
        private readonly string _queueName;
        private readonly string _fileShareName;

        public BabyShopFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BabyShopFunctions>();
            _connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString")
                                ?? throw new InvalidOperationException("Azure Storage connection string not found.");

            _blobContainerName = Environment.GetEnvironmentVariable("AzureStorage__BlobContainerName") ?? "productimages";
            _queueName = Environment.GetEnvironmentVariable("AzureStorage__QueueName") ?? "orderqueue";
            _fileShareName = Environment.GetEnvironmentVariable("AzureStorage__FileShareName") ?? "contracts";
        }

        [Function("storetotable")]
        public async Task<HttpResponseData> StoreToTable(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "storetotable")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);

                // Build values carefully — avoid DTO classes entirely
                var orderId = json.TryGetProperty("RowKey", out var rk) ? rk.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                var partitionKey = "Order";
                var customerId = json.TryGetProperty("CustomerId", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                var productId = json.TryGetProperty("ProductId", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                var quantity = json.TryGetProperty("Quantity", out var q) && q.TryGetInt32(out var qv) ? qv : 0;
                var orderDate = json.TryGetProperty("OrderDate", out var od) && od.ValueKind == JsonValueKind.String && DateTime.TryParse(od.GetString(), out var dt) ? dt : DateTime.UtcNow;

                var tableClient = new TableClient(_connectionString, "BabyOrders");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity(partitionKey, orderId)
                {
                    ["CustomerId"] = customerId,
                    ["ProductId"] = productId,
                    ["Quantity"] = quantity,
                    ["OrderDate"] = orderDate
                };

                await tableClient.AddEntityAsync(entity);

                // queue
                var queueClient = new QueueClient(_connectionString, _queueName);
                await queueClient.CreateIfNotExistsAsync();

                var messageObj = new Dictionary<string, object>
                {
                    ["OrderId"] = orderId,
                    ["CustomerId"] = customerId,
                    ["ProductId"] = productId,
                    ["Quantity"] = quantity,
                    ["OrderDate"] = orderDate
                };

                await queueClient.SendMessageAsync(JsonSerializer.Serialize(messageObj));

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(orderId);
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "storetotable error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

       
    }
}
