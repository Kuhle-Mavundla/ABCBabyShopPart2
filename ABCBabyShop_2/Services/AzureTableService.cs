using Azure;
using Azure.Data.Tables;
using ABCBabyShop_2.Models;

namespace ABCBabyShop_2.Services
{
    public class AzureTableService
    {
        private readonly TableServiceClient _serviceClient;

        public AzureTableService(IConfiguration config)
        {
            _serviceClient = new TableServiceClient(config["AzureStorage:ConnectionString"]);
        }

        private TableClient GetTableClient(string tableName)
        {
            var client = _serviceClient.GetTableClient(tableName);
            client.CreateIfNotExists();
            return client;
        }

        public void AddEntity<T>(T entity, string tableName) where T : class, ITableEntity
        {
            var table = GetTableClient(tableName);
            table.AddEntity(entity);
        }

        public IEnumerable<T> GetAllEntities<T>(string tableName) where T : class, ITableEntity, new()
        {
            var table = GetTableClient(tableName);
            return table.Query<T>();
        }

        public void DeleteEntity(string tableName, string partitionKey, string rowKey)
        {
            var table = GetTableClient(tableName);
            table.DeleteEntity(partitionKey, rowKey);
        }
    }
}
