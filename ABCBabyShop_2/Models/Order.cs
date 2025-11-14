using System.ComponentModel.DataAnnotations;

namespace ABCBabyShop_3.Models
{
    public class Order
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? CustomerId { get; set; }

        public string? ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }
}
