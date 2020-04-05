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
        /// �R���X�g���N�^
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
        /// ��������
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Initialize")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("�����������J�n");

            {
                log.LogInformation(await CreateCosmosDbDatabaseIfNotExistsAsync(_cosmosDbClient, _settings.DatabaseId)
                    ? $"CosmosDB�̃f�[�^�x�[�X���쐬���܂����B �f�[�^�x�[�X��:`{_settings.DatabaseId}`"
                    : $"�f�[�^�x�[�X��: `{_settings.DatabaseId}` �͂��łɑ��݂��܂��B");

                var indexPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true
                };

                //IncludePath�̎w��
                indexPolicy.IncludedPaths.Add(new IncludedPath
                {
                    Path = $"/*"
                });

                ////ExcludePath�̎w��
                indexPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/name/?" });

                //UniqueKey�̎w��
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
                    ? $"CosmosDB�̃R���e�i���쐬���܂����B �R���e�i��:`{_settings.ContainerId}`"
                    : $"�f�[�^�x�[�X��: `{_settings.ContainerId}` �͂��łɑ��݂��܂��B");
            }

            log.LogInformation("�����������������܂����B");

            return new OkObjectResult("");
        }

        /// <summary>
        /// CosmosDB�̃f�[�^�x�[�X���쐬����(���ɂ���ꍇ�͉������Ȃ�)
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
        /// CosmosDB�̃R���e�i���쐬(���ɂ���ꍇ�͉������Ȃ�)
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
                //�f�[�^�̗L������(�b)
                //DefaultTimeToLive = 30
            };

            //�C���f�b�N�X�|���V�[
            properties.IndexingPolicy = indexPolicy;

            // ���j�[�N�L�[
            uniqueKeys ??= new Collection<UniqueKey>();
            if (uniqueKeys.Any())
            {
                foreach (var key in uniqueKeys)
                {
                    properties.UniqueKeyPolicy.UniqueKeys.Add(key);
                }
            }

            //�R���e�i�̍쐬
            var result = await cosmosDbClient.GetDatabase(databaseId).CreateContainerIfNotExistsAsync(properties, throughput);

            return result.StatusCode == HttpStatusCode.Created;
        }
    }
}
