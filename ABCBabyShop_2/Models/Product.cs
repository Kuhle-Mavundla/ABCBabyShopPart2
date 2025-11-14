using System.ComponentModel.DataAnnotations;

namespace ABCBabyShop_3.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? ProductName { get; set; }

        public double Price { get; set; }

        public string? ImageUrl { get; set; }
    }
}
