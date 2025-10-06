using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HttpFunctions
{
    
    public class OrderQueueMessage
    {
        public string? OrderId { get; set; }
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class QueueFunctions
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _queueName;

        public QueueFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueFunctions>();
            _connectionString = Environment.GetEnvironmentVariable("AzureStorage:ConnectionString")
                                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                ?? throw new InvalidOperationException("Storage connection string not found.");
            _queueName = Environment.GetEnvironmentVariable("AzureStorage:QueueName") ?? "orderqueue";
        }

        [Function("ProcessQueueMessage")]
        public async Task ProcessQueueMessage(
            [QueueTrigger("%AzureStorage:QueueName%", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queueItem))
                {
                    _logger.LogWarning("Empty queue message received.");
                    return;
                }

                var order = JsonSerializer.Deserialize<OrderQueueMessage>(queueItem, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (order == null)
                {
                    _logger.LogWarning("Queue message could not be deserialized: {queueItem}", queueItem);
                    return;
                }

                var tableClient = new TableClient(_connectionString, "ProcessedOrders");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity("ProcessedOrder", order.OrderId ?? Guid.NewGuid().ToString())
                {
                    ["CustomerId"] = order.CustomerId ?? string.Empty,
                    ["ProductId"] = order.ProductId ?? string.Empty,
                    ["Quantity"] = order.Quantity,
                    ["OrderDate"] = order.OrderDate
                };

                await tableClient.AddEntityAsync(entity);

                _logger.LogInformation("Processed order {OrderId} from queue.", order.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue message: {queueItem}", queueItem);
            }
        }
    }
}
