namespace AzureCosmosDbFunc.Entities
{
    public class Configuration
    {
        public string AccountEndpoint { get; set; }

        public string AccountKey { get; set; }

        public string Throughput { get; set; } = "400";

        public string PartitionKey { get; set; } = "/UserData";

        public string DatabaseId { get; set; } = "AzureFunctionsDbId";

        public string ContainerId { get; set; } = "AzureFunctionsContainerId";
    }
}
