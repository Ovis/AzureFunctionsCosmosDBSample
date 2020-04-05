using System;
using System.Threading.Tasks;
using AzureCosmosDbFunc.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureCosmosDbFunc.Functions
{
    public class Delete
    {
        private readonly Configuration _settings;
        private readonly CosmosClient _cosmosDbClient;
        private readonly Container _container;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosDbClient"></param>
        public Delete(IOptions<Configuration> options, CosmosClient cosmosDbClient)
        {
            _settings = options.Value;
            _cosmosDbClient = cosmosDbClient;

            _container = _cosmosDbClient.GetContainer(_settings.DatabaseId, _settings.ContainerId);
        }

        /// <summary>
        /// 削除処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Delete")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string id = req.Query["id"];

            try
            {
                await _container.DeleteItemAsync<User>(id, new PartitionKey("UserData"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return new OkObjectResult("");
        }
    }
}
