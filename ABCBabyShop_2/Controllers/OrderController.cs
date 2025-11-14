
using ABCBabyShop_3.Models;
using ABCBabyShop_3.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCBabyShop_3.Controllers
{
    public class OrderController : Controller
    {
        private readonly AzureSqlService _sql;
        private readonly AzureQueueService _queue;
        private const string SessionCartKey = "CartItems";

        public OrderController(AzureSqlService sql, AzureQueueService queue)
        {
            _sql = sql;
            _queue = queue;
        }

        public async Task<IActionResult> Index(string searchCustomer, string searchProduct, DateTime? startDate, DateTime? endDate)
        {
            var orders = await _sql.GetAllOrdersAsync();

            if (!string.IsNullOrWhiteSpace(searchCustomer))
                orders = orders.Where(o => !string.IsNullOrEmpty(o.CustomerId) && o.CustomerId.Contains(searchCustomer, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(searchProduct))
                orders = orders.Where(o => !string.IsNullOrEmpty(o.ProductId) && o.ProductId.Contains(searchProduct, StringComparison.OrdinalIgnoreCase)).ToList();

            if (startDate.HasValue)
                orders = orders.Where(o => o.OrderDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                orders = orders.Where(o => o.OrderDate <= endDate.Value).ToList();

            return View(orders);
        }

        // Checkout action reads cart from session and creates orders
        public async Task<IActionResult> Checkout()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (string.IsNullOrWhiteSpace(customerId))
            {
                // enforce login/registration
                return RedirectToAction("Login", "Customer");
            }

            var cartJson = HttpContext.Session.GetString(SessionCartKey);
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            if (!cart.Any()) return RedirectToAction("Index", "Product");

            var createdOrders = new List<Order>();

            foreach (var item in cart)
            {
                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    OrderDate = DateTime.UtcNow
                };

                await _sql.AddOrderAsync(order);
                createdOrders.Add(order);

                // Send queue message (JSON string)
                var msg = JsonSerializer.Serialize(new
                {
                    OrderId = order.Id,
                    order.CustomerId,
                    order.ProductId,
                    order.Quantity,
                    order.OrderDate
                });

                await _queue.SendMessageAsync(msg);
            }

            // Clear cart
            HttpContext.Session.Remove(SessionCartKey);

            return View("CheckoutSuccess", createdOrders);
        }

        public async Task<IActionResult> Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            order.Id = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.UtcNow;

            await _sql.AddOrderAsync(order);

            var message = JsonSerializer.Serialize(new
            {
                OrderId = order.Id,
                order.CustomerId,
                order.ProductId,
                order.Quantity,
                order.OrderDate
            });

            await _queue.SendMessageAsync(message);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _sql.DeleteOrderAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private class CartItem
        {
            public string ProductId { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}
