using ABCBabyShop_2.Models;
using ABCBabyShop_2.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCBabyShop_2.Controllers
{
    public class OrderController : Controller
    {
        private readonly AzureTableService _tableService;
        private readonly AzureQueueService _queueService;
 

        public OrderController(AzureTableService tableService, AzureQueueService queueService)
        {
            _tableService = tableService;
            _queueService = queueService;
          
        }

        public IActionResult Index()
        {
            var orders = _tableService.GetAllEntities<Order>("Order");
            return View(orders);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            order.RowKey = Guid.NewGuid().ToString();


          
            _tableService.AddEntity(order, "Order");

          
            var message = JsonSerializer.Serialize(new
            {
                OrderId = order.RowKey,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                OrderDate = order.OrderDate
            });
            await _queueService.SendMessageAsync(message);

            return RedirectToAction("Index");
        }

        public IActionResult Delete(string rowKey)
        {
            _tableService.DeleteEntity("Order", "Order", rowKey);
            return RedirectToAction("Index");
        }
    }
}
