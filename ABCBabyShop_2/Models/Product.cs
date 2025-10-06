using Azure;
using Azure.Data.Tables;

namespace ABCBabyShop_2.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } // Unique ID
        public string? ProductName { get; set; }
        public double Price { get; set; }
        public string? ImageUrl { get; set; } // Blob URL
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
