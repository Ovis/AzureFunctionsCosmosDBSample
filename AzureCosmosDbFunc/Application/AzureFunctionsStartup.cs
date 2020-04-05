using AzureCosmosDbFunc.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AzureCosmosDbFunc.Application.AzureFunctionsStartup))]
namespace AzureCosmosDbFunc.Application
{
    public class AzureFunctionsStartup : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<Configuration>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Options").Bind(settings);
                })
                .Services.AddSingleton((provider) =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();

                    var accountEndpoint = configuration.GetValue<string>("Options:AccountEndpoint");
                    var accountKey = configuration.GetValue<string>("Options:AccountKey");

                    CosmosClientBuilder cosmosClientBuilder = new CosmosClientBuilder(accountEndpoint, accountKey);

                    return cosmosClientBuilder.WithConnectionModeDirect()
                        .WithApplicationRegion(Regions.JapanEast)
                        .WithBulkExecution(true)
                        .WithConnectionModeDirect()
                        .Build();
                });
        }
    }
}
