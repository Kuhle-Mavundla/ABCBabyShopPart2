using Azure;
using Azure.Data.Tables;

namespace ABCBabyShop_2.Models
{
    //Declaration of Customer class implementing ITableEntity for Azure Table Storage
    public class Customer : ITableEntity
    {
        
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } // Unique ID
        public string? CustomerName { get; set; }
        public string? Email { get; set; }

        public string? Password { get; set; }

        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
