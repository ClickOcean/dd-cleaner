using System.Text.Json.Serialization;

public class MetricResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
