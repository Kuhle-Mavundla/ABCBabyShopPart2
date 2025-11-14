using ABCBabyShop_3.Data;
using ABCBabyShop_3.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCBabyShop_3.Services
{
    public class AzureSqlService
    {
        private readonly ApplicationDbContext _db;

        public AzureSqlService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Customers
        public async Task<List<Customer>> GetAllCustomersAsync() =>
            await _db.Customers.AsNoTracking().ToListAsync();

        public async Task<Customer?> GetCustomerByIdAsync(string id) =>
            await _db.Customers.FindAsync(id);

        public async Task AddCustomerAsync(Customer c)
        {
            _db.Customers.Add(c);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(string id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c != null)
            {
                _db.Customers.Remove(c);
                await _db.SaveChangesAsync();
            }
        }

        // Products
        public async Task<List<Product>> GetAllProductsAsync() =>
            await _db.Products.AsNoTracking().ToListAsync();

        public async Task<Product?> GetProductByIdAsync(string id) =>
            await _db.Products.FindAsync(id);

        public async Task AddProductAsync(Product p)
        {
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(string id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null)
            {
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
            }
        }

        // Orders
        public async Task<List<Order>> GetAllOrdersAsync() =>
            await _db.Orders.AsNoTracking().ToListAsync();

        public async Task AddOrderAsync(Order o)
        {
            _db.Orders.Add(o);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteOrderAsync(string id)
        {
            var o = await _db.Orders.FindAsync(id);
            if (o != null)
            {
                _db.Orders.Remove(o);
                await _db.SaveChangesAsync();
            }
        }
    }
}
