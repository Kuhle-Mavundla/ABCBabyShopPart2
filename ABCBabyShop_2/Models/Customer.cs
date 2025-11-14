using System.ComponentModel.DataAnnotations;

namespace ABCBabyShop_3.Models
{
    public class Customer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string? CustomerName { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; } // For POE: keep simple (hashed ideally in prod)
    }
}
