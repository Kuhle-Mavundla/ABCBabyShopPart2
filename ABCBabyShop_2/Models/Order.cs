using Azure;
using Azure.Data.Tables;

namespace ABCBabyShop_2.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } // Unique ID
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
