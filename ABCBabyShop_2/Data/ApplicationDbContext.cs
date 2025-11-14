
using ABCBabyShop_3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ABCBabyShop_3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
    }
}
