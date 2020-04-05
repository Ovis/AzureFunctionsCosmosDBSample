namespace AzureCosmosDbFunc.Entities
{
    public class UserDataModel
    {
        public string Id { get; set; }

        // やること
        public Name Name { get; set; }

        public int Age { get; set; }

        public string PartitionKey { get; set; } = "UserData";
    }

    public class Name
    {
        public string GivenName { get; set; }

        public string FamilyName { get; set; }

        public string FullName => FamilyName + GivenName;
    }
}
