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
    public class Create
    {
        private readonly Configuration _settings;
        private readonly CosmosClient _cosmosDbClient;
        private readonly Container _container;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosDbClient"></param>
        public Create(IOptions<Configuration> options, CosmosClient cosmosDbClient)
        {
            _settings = options.Value;
            _cosmosDbClient = cosmosDbClient;

            _container = _cosmosDbClient.GetContainer(_settings.DatabaseId, _settings.ContainerId);
        }

        [FunctionName("Create")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CosmosDBにデータを登録");

            var document = new UserDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Name = new Name
                {
                    FamilyName = "Yamada",
                    GivenName = "Taro"
                },
                Age = 20
            };

            try
            {
                await _container.CreateItemAsync(document);
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
                return new BadRequestErrorMessageResult($"{e.Message}");
            }


            return new OkObjectResult($"Success.Id='{document.Id}'");
        }
    }
}
