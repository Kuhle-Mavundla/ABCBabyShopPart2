using ABCBabyShop_2.Models;
using Microsoft.AspNetCore.Mvc;
using ABCBabyShop_2.Services;

namespace ABCBabyShop_2.Controllers
{
    public class ProductController : Controller
    {
        private readonly AzureTableService _tableService;
        private readonly AzureBlobService _blobService;
    

        public ProductController(AzureTableService tableService, AzureBlobService blobService)
        {
            _tableService = tableService;
            _blobService = blobService;
          
        }

        public IActionResult Index()
        {
            var products = _tableService.GetAllEntities<Product>("Product");
            return View(products);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile image)
        {
            if (image != null)
            {
                using var stream = image.OpenReadStream();
                // Direct upload to Blob Storage
                product.ImageUrl = await _blobService.UploadFileAsync(image);
            }

            product.RowKey = Guid.NewGuid().ToString();
            _tableService.AddEntity(product, "Product");
            return RedirectToAction("Index");
        }

        public IActionResult Delete(string rowKey)
        {
            _tableService.DeleteEntity("Product", "Product", rowKey);
            return RedirectToAction("Index");
        }
        public IActionResult Index(string search)
        {
            var products = _tableService.GetAllEntities<Product>("Product");

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                products = products.Where(p =>
                    (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(p.PartitionKey) && p.PartitionKey.ToLower().Contains(search))                   
                ).ToList();
            }

            return View(products);
        }

    }
}
