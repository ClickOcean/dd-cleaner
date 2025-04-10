using System.Text.Json.Serialization;

public class MetricsTagsResponse
{
    [JsonPropertyName("data")]
    public MetricsTagsData Data { get; set; }
}
