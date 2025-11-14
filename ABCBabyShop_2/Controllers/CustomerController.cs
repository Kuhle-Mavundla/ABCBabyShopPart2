using ABCBabyShop_3.Models;
using ABCBabyShop_3.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCBabyShop_3.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AzureSqlService _sql;

        public CustomerController(AzureSqlService sql)
        {
            _sql = sql;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _sql.GetAllCustomersAsync();
            return View(customers);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            // Basic validation already provided by model attributes
            await _sql.AddCustomerAsync(customer);
            // After registration redirect to Login
            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _sql.DeleteCustomerAsync(id);
            return RedirectToAction("Index");
        }

        // Login GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login POST
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password required.");
                return View();
            }

            var customers = await _sql.GetAllCustomersAsync();
            var user = customers.FirstOrDefault(c =>
                string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase)
                && c.Password == password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid credentials.");
                return View();
            }

            // Save minimal customer data in session
            HttpContext.Session.SetString("CustomerId", user.Id);
            HttpContext.Session.SetString("CustomerName", user.CustomerName ?? string.Empty);

            return RedirectToAction("Index", "Product");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CustomerId");
            HttpContext.Session.Remove("CustomerName");
            return RedirectToAction(nameof(Login));
        }
    }
}
