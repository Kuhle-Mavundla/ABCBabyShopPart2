using ABCBabyShop_2.Services;
using ABCBabyShop_2.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCBabyShop_2.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AzureTableService _tableService;

        public CustomerController(AzureTableService tableService)
        {
            _tableService = tableService;
        }

        public IActionResult Index()
        {
            var customers = _tableService.GetAllEntities<Customer>("Customer");
            return View(customers);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Customer customer)
        {
            customer.RowKey = Guid.NewGuid().ToString();
            _tableService.AddEntity(customer, "Customer");
            return RedirectToAction("Index");
        }

        public IActionResult Delete(string rowKey)
        {
            _tableService.DeleteEntity("Customer", "Customer", rowKey);
            return RedirectToAction("Index");
        }
    }
}
