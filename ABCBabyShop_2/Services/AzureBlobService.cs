using Azure.Storage.Blobs;

namespace ABCBabyShop_2.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var containerName = config["AzureStorage:BlobContainerName"];
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var blobClient = _containerClient.GetBlobClient(file.FileName);
            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);
            return blobClient.Uri.ToString();
        }
    }
}
