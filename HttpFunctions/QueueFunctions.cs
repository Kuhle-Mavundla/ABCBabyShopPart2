using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCBabyShop_3.Functions
{
    public class QueueFunctions
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _queueName;

        public QueueFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueFunctions>();
            _connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString")
                                ?? throw new InvalidOperationException("Storage connection string not found.");
            _queueName = Environment.GetEnvironmentVariable("AzureStorage__QueueName") ?? "orderqueue";
        }

        [Function("ProcessQueueMessage")]
        public async Task ProcessQueueMessage([QueueTrigger("%AzureStorage__QueueName%", Connection = "AzureStorage__ConnectionString")] string queueItem)
        {
            if (string.IsNullOrWhiteSpace(queueItem))
            {
                _logger.LogWarning("Empty queue message received.");
                return;
            }

            try
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(queueItem);
                var orderId = doc.TryGetProperty("OrderId", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                var customerId = doc.TryGetProperty("CustomerId", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                var productId = doc.TryGetProperty("ProductId", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                var quantity = doc.TryGetProperty("Quantity", out var q) && q.TryGetInt32(out var qv) ? qv : 0;
                var orderDate = doc.TryGetProperty("OrderDate", out var od) && od.ValueKind == JsonValueKind.String && DateTime.TryParse(od.GetString(), out var dt) ? dt : DateTime.UtcNow;

                var tableClient = new TableClient(_connectionString, "ProcessedOrders");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity("ProcessedOrder", orderId)
                {
                    ["CustomerId"] = customerId,
                    ["ProductId"] = productId,
                    ["Quantity"] = quantity,
                    ["OrderDate"] = orderDate,
                    ["RawMessage"] = queueItem
                };

                await tableClient.AddEntityAsync(entity);

                _logger.LogInformation("Processed order {OrderId} from queue.", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue message: {queueItem}", queueItem);
            }
        }
    }
}
