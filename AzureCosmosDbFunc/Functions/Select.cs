using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using AzureCosmosDbFunc.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureCosmosDbFunc.Functions
{
    public class Select
    {
        private readonly Configuration _settings;
        private readonly CosmosClient _cosmosDbClient;
        private readonly Container _container;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosDbClient"></param>
        public Select(IOptions<Configuration> options, CosmosClient cosmosDbClient)
        {
            _settings = options.Value;
            _cosmosDbClient = cosmosDbClient;

            _container = _cosmosDbClient.GetContainer(_settings.DatabaseId, _settings.ContainerId);
        }

        /// <summary>
        /// 指定IDによる取得処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SelectItem")]
        public async Task<IActionResult> Run1(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var id = req.Query["id"];

            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestErrorMessageResult("Error.Please input ID.");
            }

            try
            {
                var document = await _container.ReadItemAsync<UserDataModel>(id, new PartitionKey("UserData"));
                return new OkObjectResult(
                    $"ID:{document.Resource.Id} Name:{document.Resource.Name.FullName} Age:{document.Resource.Age}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new BadRequestErrorMessageResult("");


        }

        /// <summary>
        /// SQLを利用した取得処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SelectSql")]
        public async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var queryRequestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey("UserData") };

            var iterator = _container.GetItemQueryIterator<UserDataModel>("SELECT * FROM c WHERE c.age > 10", requestOptions: queryRequestOptions);

            var returnValue = "";
            do
            {
                var result = await iterator.ReadNextAsync();

                foreach (var item in result)
                {
                    returnValue += $"ID:{item.Id} Name:{item.Name.FullName} Age:{item.Age} {Environment.NewLine}";
                }
            } while (iterator.HasMoreResults);
            Console.WriteLine($"{returnValue}");

            return new OkObjectResult(returnValue);
        }


        /// <summary>
        /// LINQを利用した取得処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SelectLinq")]
        public async Task<IActionResult> Run3(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var queryRequestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey("UserData") };

            var iterator = _container.GetItemLinqQueryable<UserDataModel>(requestOptions: queryRequestOptions)
                .Where(x => x.Age > 10)
                .ToFeedIterator();

            var returnValue = "";
            do
            {
                var result = await iterator.ReadNextAsync();

                foreach (var item in result)
                {
                    returnValue += $"ID:{item.Id} Name:{item.Name.FullName} Age:{item.Age} {Environment.NewLine}";
                }
            } while (iterator.HasMoreResults);

            Console.WriteLine($"{returnValue}");

            return new OkObjectResult(returnValue);
        }
    }
}
