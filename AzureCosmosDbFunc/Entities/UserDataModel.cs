using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AzureCosmosDbFunc.Entities
{
    public class UserDataModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        // やること
        [JsonProperty(PropertyName = "fullName")]
        public Name Name { get; set; }

        [JsonProperty(PropertyName = "age")]
        public int Age { get; set; }

        [JsonProperty(PropertyName = "personalId")]
        public string PersonalId { get; set; } = Guid.NewGuid().ToString();

        public string PartitionKey { get; set; } = "UserData";
    }

    public class Name
    {
        [JsonPropertyName("givenName")]
        public string GivenName { get; set; }

        [JsonPropertyName("familyName")]
        public string FamilyName { get; set; }

        public string FullName => FamilyName + GivenName;
    }
}
