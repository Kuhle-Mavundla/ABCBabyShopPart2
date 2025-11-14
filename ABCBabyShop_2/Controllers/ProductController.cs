using ABCBabyShop_3.Services;
using ABCBabyShop_3.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCBabyShop_3.Controllers
{
    public class ProductController : Controller
    {
        private readonly AzureSqlService _sql;
        private readonly AzureBlobService _blob;

        private const string SessionCartKey = "CartItems";

        public ProductController(AzureSqlService sql, AzureBlobService blob)
        {
            _sql = sql;
            _blob = blob;
        }

        public async Task<IActionResult> Index(string search)
        {
            var products = await _sql.GetAllProductsAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                products = products.Where(p =>
                    (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(p.Id) && p.Id.ToLower().Contains(search))
                ).ToList();
            }

            return View(products);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? image)
        {
            if (image != null)
            {
                product.ImageUrl = await _blob.UploadFileAsync(image);
            }

            await _sql.AddProductAsync(product);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _sql.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Add product to session-based cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            var product = await _sql.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            var cartJson = HttpContext.Session.GetString(SessionCartKey);
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductId = productId, Quantity = quantity });
            }

            HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cart));

            return RedirectToAction(nameof(Index));
        }

        // Show Cart
        public async Task<IActionResult> Cart()
        {
            var cartJson = HttpContext.Session.GetString(SessionCartKey);
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();

            var items = new List<(Product Product, int Quantity)>();
            foreach (var c in cart)
            {
                var p = await _sql.GetProductByIdAsync(c.ProductId);
                if (p != null) items.Add((p, c.Quantity));
            }

            ViewData["CartItems"] = items;
            return View();
        }

        private class CartItem
        {
            public string ProductId { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }
    }
}
