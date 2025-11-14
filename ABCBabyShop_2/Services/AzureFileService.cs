using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCBabyShop_3.Services
{
    public class AzureFileService
    {
        private readonly ShareClient _shareClient;

        public AzureFileService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"];
            string fileShareName = configuration["AzureStorage:FileShareName"];

            _shareClient = new ShareClient(connectionString, fileShareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadFileAsync(string fileName, Stream fileStream)
        {
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareFileClient file = rootDir.GetFileClient(fileName);
            await file.CreateAsync(fileStream.Length);
            await file.UploadAsync(fileStream);
        }

        public async Task<List<string>> ListFilesAsync()
        {
            List<string> files = new List<string>();
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();

            await foreach (ShareFileItem item in rootDir.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                    files.Add(item.Name);
            }

            return files;
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareFileClient file = rootDir.GetFileClient(fileName);
            var response = await file.DownloadAsync();
            return response.Value.Content;
        }
    }
}
