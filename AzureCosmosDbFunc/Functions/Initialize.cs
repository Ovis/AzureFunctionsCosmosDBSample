using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
    public class Initialize
    {
        private readonly Configuration _settings;
        private readonly CosmosClient _cosmosDbClient;
        private readonly Database _cosmosDatabase;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cosmosDbClient"></param>
        public Initialize(IOptions<Configuration> options, CosmosClient cosmosDbClient)
        {
            _settings = options.Value;
            _cosmosDbClient = cosmosDbClient;

            _cosmosDatabase = _cosmosDbClient.GetDatabase(_settings.DatabaseId);
        }

        /// <summary>
        /// 初期処理
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Initialize")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("初期処理を開始");

            {
                log.LogInformation(await CreateCosmosDbDatabaseIfNotExistsAsync(_cosmosDbClient, _settings.DatabaseId)
                    ? $"CosmosDBのデータベースを作成しました。 データベース名:`{_settings.DatabaseId}`"
                    : $"データベース名: `{_settings.DatabaseId}` はすでに存在します。");

                var indexPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true
                };

                //IncludePathの指定
                indexPolicy.IncludedPaths.Add(new IncludedPath
                {
                    Path = $"/*"
                });

                ////ExcludePathの指定
                indexPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/name/?" });

                //UniqueKeyの指定
                var uniqueKeys =
                          new Collection<UniqueKey>
                          {
                              new UniqueKey
                              {
                                  Paths = { "/personalId" }
                              }
                          };

                int.TryParse(_settings.Throughput, out var throughput);

                var createContainerResult = await CreateCosmosDbContainerIfNotExistsAsync(
                    _cosmosDbClient,
                    _settings.DatabaseId,
                    _settings.ContainerId,
                    throughput: throughput,
                    partitionKeyPath: _settings.PartitionKey,
                    indexPolicy: indexPolicy,
                    uniqueKeys: uniqueKeys);

                log.LogInformation(createContainerResult
                    ? $"CosmosDBのコンテナを作成しました。 コンテナ名:`{_settings.ContainerId}`"
                    : $"データベース名: `{_settings.ContainerId}` はすでに存在します。");
            }

            log.LogInformation("初期処理が完了しました。");

            return new OkObjectResult("");
        }

        /// <summary>
        /// CosmosDBのデータベースを作成する(既にある場合は何もしない)
        /// </summary>
        /// <param name="cosmosDbClient"></param>
        /// <param name="databaseId"></param>
        /// <returns></returns>
        private static async Task<bool> CreateCosmosDbDatabaseIfNotExistsAsync(CosmosClient cosmosDbClient, string databaseId)
        {
            var result = await cosmosDbClient.CreateDatabaseIfNotExistsAsync(databaseId, 400); ;

            return result.StatusCode == HttpStatusCode.Created;

        }

        /// <summary>
        /// CosmosDBのコンテナを作成(既にある場合は何もしない)
        /// </summary>
        /// <param name="cosmosDbClient"></param>
        /// <param name="databaseId"></param>
        /// <param name="containerId"></param>
        /// <param name="throughput"></param>
        /// <param name="partitionKeyPath"></param>
        /// <param name="indexPolicy"></param>
        /// <param name="uniqueKeys"></param>
        /// <returns></returns>
        private static async Task<bool> CreateCosmosDbContainerIfNotExistsAsync(CosmosClient cosmosDbClient,
            string databaseId,
            string containerId,
            int throughput = 400,
            string partitionKeyPath = "",
            IndexingPolicy indexPolicy = null,
            Collection<UniqueKey> uniqueKeys = null
        )
        {
            var properties = new ContainerProperties(containerId, partitionKeyPath)
            {
                //データの有効期限(秒)
                //DefaultTimeToLive = 30
            };

            //インデックスポリシー
            properties.IndexingPolicy = indexPolicy;

            // ユニークキー
            uniqueKeys ??= new Collection<UniqueKey>();
            if (uniqueKeys.Any())
            {
                foreach (var key in uniqueKeys)
                {
                    properties.UniqueKeyPolicy.UniqueKeys.Add(key);
                }
            }

            //コンテナの作成
            var result = await cosmosDbClient.GetDatabase(databaseId).CreateContainerIfNotExistsAsync(properties, throughput);

            return result.StatusCode == HttpStatusCode.Created;
        }
    }
}
