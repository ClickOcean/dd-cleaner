using System.Text.Json.Serialization;

public class MetricsResponse
{
    [JsonPropertyName("metrics")]
    public List<string> Metrics { get; set; } = [];
}
