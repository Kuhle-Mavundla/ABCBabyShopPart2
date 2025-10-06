using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCBabyShop.Functions
{
    // DTO for baby product order
    public class BabyOrderDto
    {
        public string? PartitionKey { get; set; } = "Order";
        public string? RowKey { get; set; }
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }

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
            _connectionString = Environment.GetEnvironmentVariable("AzureStorage:ConnectionString")
                                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                ?? throw new InvalidOperationException("Azure Storage connection string not found.");

            _blobContainerName = Environment.GetEnvironmentVariable("AzureStorage:BlobContainerName") ?? "babyproductimages";
            _queueName = Environment.GetEnvironmentVariable("AzureStorage:QueueName") ?? "babyorderqueue";
            _fileShareName = Environment.GetEnvironmentVariable("AzureStorage:FileShareName") ?? "babyorders";
        }

        // -------------------- POST /storetotable --------------------
        [Function("storetotable")]
        public async Task<HttpResponseData> StoreToTable(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "storetotable")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonSerializer.Deserialize<BabyOrderDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (order == null)
                {
                    res.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await res.WriteStringAsync("Invalid JSON payload for baby order.");
                    return res;
                }

                order.RowKey = string.IsNullOrWhiteSpace(order.RowKey) ? Guid.NewGuid().ToString() : order.RowKey;
                order.PartitionKey = string.IsNullOrWhiteSpace(order.PartitionKey) ? "Order" : order.PartitionKey;
                order.OrderDate = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate;

                var tableClient = new TableClient(_connectionString, "BabyOrders");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity(order.PartitionKey, order.RowKey)
                {
                    ["CustomerId"] = order.CustomerId ?? string.Empty,
                    ["ProductId"] = order.ProductId ?? string.Empty,
                    ["ProductName"] = order.ProductName ?? string.Empty,
                    ["Quantity"] = order.Quantity,
                    ["OrderDate"] = order.OrderDate
                };

                await tableClient.AddEntityAsync(entity);

                // Add message to queue
                var queueClient = new QueueClient(_connectionString, _queueName);
                await queueClient.CreateIfNotExistsAsync();

                var messageObj = new
                {
                    OrderId = order.RowKey,
                    order.CustomerId,
                    order.ProductId,
                    order.ProductName,
                    order.Quantity,
                    order.OrderDate
                };

                await queueClient.SendMessageAsync(JsonSerializer.Serialize(messageObj));

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(order.RowKey);
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

        // -------------------- GET /getorders --------------------
        [Function("getorders")]
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getorders")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                var tableClient = new TableClient(_connectionString, "BabyOrders");
                await tableClient.CreateIfNotExistsAsync();

                var orders = new List<Dictionary<string, object>>();
                await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                {
                    orders.Add(entity.ToDictionary());
                }

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true }));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getorders error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

        // -------------------- GET /processqueue --------------------
        [Function("processqueue")]
        public async Task<HttpResponseData> ProcessQueue(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "processqueue")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                var queueClient = new QueueClient(_connectionString, _queueName);
                await queueClient.CreateIfNotExistsAsync();

                var tableClient = new TableClient(_connectionString, "ProcessedBabyOrders");
                await tableClient.CreateIfNotExistsAsync();

                // Preload queue if empty
                var peek = await queueClient.PeekMessagesAsync(1);
                if (!peek.Value.Any())
                {
                    var preloadOrder = new
                    {
                        OrderId = Guid.NewGuid().ToString(),
                        CustomerId = "TestCustomer",
                        ProductId = "BabyBottle001",
                        ProductName = "Baby Bottle",
                        Quantity = 1,
                        OrderDate = DateTime.UtcNow
                    };
                    await queueClient.SendMessageAsync(JsonSerializer.Serialize(preloadOrder));
                }

                var messages = await queueClient.ReceiveMessagesAsync(10);
                var processedList = new List<object>();

                foreach (var msg in messages.Value)
                {
                    try
                    {
                        var doc = JsonSerializer.Deserialize<JsonElement>(msg.MessageText);
                        string orderId = doc.TryGetProperty("OrderId", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                        var entity = new TableEntity("Processed", orderId)
                        {
                            ["RawMessage"] = msg.MessageText,
                            ["ProcessedAt"] = DateTime.UtcNow
                        };
                        await tableClient.AddEntityAsync(entity);

                        processedList.Add(new { OrderId = orderId, RawMessage = msg.MessageText });

                        await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
                    }
                    catch (Exception exInner)
                    {
                        _logger.LogError(exInner, "Error processing queue message (skipped to prevent poison queue)");
                    }
                }

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(JsonSerializer.Serialize(processedList, new JsonSerializerOptions { WriteIndented = true }));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "processqueue error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

        // -------------------- POST /uploadblob --------------------
        [Function("uploadblob")]
        public async Task<HttpResponseData> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uploadblob")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                string fileName = req.Headers.TryGetValues("filename", out var headerVals) ? headerVals.FirstOrDefault() : $"babyproduct_{Guid.NewGuid()}.jpg";

                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                ms.Position = 0;

                var blobService = new BlobServiceClient(_connectionString);
                var container = blobService.GetBlobContainerClient(_blobContainerName);
                await container.CreateIfNotExistsAsync();

                var blobClient = container.GetBlobClient(fileName);
                ms.Position = 0;
                await blobClient.UploadAsync(ms, overwrite: true);

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(blobClient.Uri.ToString());
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "uploadblob error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

        // -------------------- GET /getblobs --------------------
        [Function("getblobs")]
        public async Task<HttpResponseData> GetBlobs(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getblobs")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                var blobService = new BlobServiceClient(_connectionString);
                var container = blobService.GetBlobContainerClient(_blobContainerName);
                await container.CreateIfNotExistsAsync();

                var blobUrls = new List<string>();
                await foreach (var blob in container.GetBlobsAsync())
                {
                    var blobClient = container.GetBlobClient(blob.Name);
                    blobUrls.Add(blobClient.Uri.ToString());
                }

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(JsonSerializer.Serialize(blobUrls, new JsonSerializerOptions { WriteIndented = true }));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getblobs error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

        // -------------------- POST /writefile --------------------
        [Function("writefile")]
        public async Task<HttpResponseData> WriteFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "writefile")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                string fileName = req.Headers.TryGetValues("filename", out var headerVals) ? headerVals.FirstOrDefault() : $"babyorder_{DateTime.UtcNow.Ticks}.txt";

                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                ms.Position = 0;

                var share = new ShareClient(_connectionString, _fileShareName);
                await share.CreateIfNotExistsAsync();
                var root = share.GetRootDirectoryClient();
                var fileClient = root.GetFileClient(fileName);

                await fileClient.CreateAsync(ms.Length);
                ms.Position = 0;
                await fileClient.UploadAsync(ms);

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync($"File '{fileName}' written to share '{_fileShareName}'.");
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "writefile error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }

        // -------------------- GET /getfiles --------------------
        [Function("getfiles")]
        public async Task<HttpResponseData> GetFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getfiles")] HttpRequestData req)
        {
            var res = req.CreateResponse();
            try
            {
                var share = new ShareClient(_connectionString, _fileShareName);
                await share.CreateIfNotExistsAsync();
                var root = share.GetRootDirectoryClient();

                var fileUrls = new List<string>();
                await foreach (var file in root.GetFilesAndDirectoriesAsync())
                {
                    if (file.IsDirectory) continue;
                    var fileClient = root.GetFileClient(file.Name);
                    fileUrls.Add(fileClient.Uri.ToString());
                }

                res.StatusCode = System.Net.HttpStatusCode.OK;
                await res.WriteStringAsync(JsonSerializer.Serialize(fileUrls, new JsonSerializerOptions { WriteIndented = true }));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getfiles error");
                res.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"Error: {ex.Message}");
                return res;
            }
        }
    }
}
