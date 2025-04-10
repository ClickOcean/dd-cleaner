using System.Text.Json.Serialization;

public class UsingMetricsResponse
{
    [JsonPropertyName("data")]
    public List<UsingMetrics> Data { get; set; } = [];
}