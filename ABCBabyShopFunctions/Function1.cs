using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABCRetailFunctions
{
    public class ProcessQueueService
    {
        private readonly ILogger<ProcessQueueService> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueClient _queueClient;
        private ILogger logger;
        private string connectionString;

        public ProcessQueueService(ILogger<ProcessQueueService> logger, string connectionString)
        {
            _logger = logger;
            _tableServiceClient = new TableServiceClient(connectionString);

            // Connect to the queue
            _queueClient = new QueueClient(connectionString, "orderqueue");
            _queueClient.CreateIfNotExists();
        }

        public ProcessQueueService(ILogger logger, string connectionString)
        {
            this.logger = logger;
            this.connectionString = connectionString;
        }

        // ✅ Main method to process messages
        public async Task<List<string>> ProcessQueueMessagesAsync()
        {
            List<string> processedMessages = new();

            // Ensure Orders table exists and preload if empty
            var ordersTable = _tableServiceClient.GetTableClient("Orders");
            await ordersTable.CreateIfNotExistsAsync();
            await PreloadOrdersIfEmptyAsync(ordersTable);

            // Receive up to 10 messages from the queue
            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 10);

            if (messages.Length == 0)
            {
                _logger.LogInformation("No messages in queue. Displaying current Orders table...");
                var allOrders = ordersTable.Query<TableEntity>();
                foreach (var order in allOrders)
                {
                    _logger.LogInformation($"Order: {order["OrderId"]} | Customer: {order["CustomerName"]} | Product: {order["Product"]} | Status: {order["Status"]}");
                }

                processedMessages.Add(" No queue messages found. Displayed existing table data instead.");
                return processedMessages;
            }

            // Process queue messages
            foreach (var msg in messages)
            {
                _logger.LogInformation($"Processing transaction message: {msg.MessageText}");
                processedMessages.Add(msg.MessageText);

                try
                {
                    var doc = JsonSerializer.Deserialize<JsonElement>(msg.MessageText);

                    if (doc.TryGetProperty("OrderId", out var orderId))
                    {
                        var processedTable = _tableServiceClient.GetTableClient("ProcessedOrders");
                        await processedTable.CreateIfNotExistsAsync();

                        var entity = new TableEntity("Processed", orderId.GetString() ?? Guid.NewGuid().ToString())
                        {
                            {"RawMessage", msg.MessageText},
                            {"ProcessedAt", DateTime.UtcNow}
                        };

                        await processedTable.AddEntityAsync(entity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queue item");
                }

                // Delete message after successful processing
                await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            }

            return processedMessages;
        }

        // ✅ Helper: Preload sample data if table empty
        private async Task PreloadOrdersIfEmptyAsync(TableClient table)
        {
            var existing = table.Query<TableEntity>().Any();
            if (existing)
            {
                _logger.LogInformation("Orders table already contains data — skipping preload.");
                return;
            }

            _logger.LogInformation("Orders table is empty. Preloading sample data...");

            string partitionKey = "OrdersPartition";
            var orders = new[]
            {
                new TableEntity(partitionKey, "Order001")
                {
                    {"OrderId", "Order001"},
                    {"CustomerName", "Alice Smith"},
                    {"Product", "Laptop"},
                    {"Quantity", 1},
                    {"TotalAmount", 12000.50},
                    {"Status", "Pending"}
                },
                new TableEntity(partitionKey, "Order002")
                {
                    {"OrderId", "Order002"},
                    {"CustomerName", "Bob Johnson"},
                    {"Product", "Smartphone"},
                    {"Quantity", 2},
                    {"TotalAmount", 15999.00},
                    {"Status", "Pending"}
                },
                new TableEntity(partitionKey, "Order003")
                {
                    {"OrderId", "Order003"},
                    {"CustomerName", "Carol White"},
                    {"Product", "Headphones"},
                    {"Quantity", 3},
                    {"TotalAmount", 2999.99},
                    {"Status", "Pending"}
                }
            };

            foreach (var order in orders)
            {
                await table.AddEntityAsync(order);
                _logger.LogInformation($"Preloaded order: {order.RowKey}");
            }

            _logger.LogInformation(" Orders table successfully preloaded.");
        }
    }
}
