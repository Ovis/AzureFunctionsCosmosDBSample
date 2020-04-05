using System;
using System.Threading.Tasks;
using System.Web.Http;
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
    public class Update
    {
        private readonly Configuration _settings;
        private readonly CosmosClient _cosmosDbClient;
        private readonly Container _container;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosDbClient"></param>
        public Update(IOptions<Configuration> options, CosmosClient cosmosDbClient)
        {
            _settings = options.Value;
            _cosmosDbClient = cosmosDbClient;

            _container = _cosmosDbClient.GetContainer(_settings.DatabaseId, _settings.ContainerId);
        }

        /// <summary>
        /// ID指定による更新処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Update1")]
        public async Task<IActionResult> Run1(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string id = req.Query["id"];
            int.TryParse(req.Query["age"], out var age);

            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestErrorMessageResult("Error.Please input ID.");
            }

            try
            {
                var document = await _container.ReadItemAsync<UserDataModel>(id, new PartitionKey("UserData"));

                var user = document.Resource;
                user.Age = age;

                await _container.ReplaceItemAsync(user, user.Id);

                return new OkObjectResult("");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new BadRequestErrorMessageResult("");
        }


        /// <summary>
        /// 上書き処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Update2")]
        public async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string id = req.Query["id"];
            int.TryParse(req.Query["age"], out var age);

            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestErrorMessageResult("Error.Please input ID.");
            }

            try
            {
                var document = await _container.ReadItemAsync<UserDataModel>(id, new PartitionKey("UserData"));

                var user = document.Resource;
                user.Age = age;

                await _container.UpsertItemAsync(user);

                return new OkObjectResult("");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new BadRequestErrorMessageResult("");
        }
    }
}
