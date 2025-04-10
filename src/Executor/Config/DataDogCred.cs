using System.Text.Json.Serialization;

public class DataDogCred
{
    [JsonPropertyName("appKey")]
    public string AppKey { get; set; }

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; }

    [JsonPropertyName("org")]
    public string Organization { get; set; }
}