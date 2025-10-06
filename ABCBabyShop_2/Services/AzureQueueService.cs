using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABCBabyShop_2.Services
{
    public class AzureQueueService
    {
        private readonly QueueClient _queueClient;

        public AzureQueueService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var queueName = config["AzureStorage:QueueName"];
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        // Optional: receive one message (for admin/testing). Not used by UI by default.
        public async Task<QueueMessage?> ReceiveMessageAsync()
        {
            var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
            var msg = response.Value.FirstOrDefault();
            if (msg != null)
            {
                // Delete after reading to avoid reprocessing unintentionally
                await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            }
            return msg;
        }
    }
}
