using Microsoft.AspNetCore.Mvc;
using ABCBabyShop_3.Services;
using ABCBabyShop_3.Models;

namespace ABCBabyShop_3.Controllers
{
    public class ContractController : Controller
    {
        private readonly AzureFileService _fileService;


        public ContractController(AzureFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            var files = await _fileService.ListFilesAsync();
            var model = files.Select(f => new Contract
            {
                FileName = f,
                FileUrl = Url.Action("Download", "Contract", new { fileName = f })
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var localStream = file.OpenReadStream();
                await _fileService.UploadFileAsync(file.FileName, localStream);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _fileService.DownloadFileAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }
    }
}
