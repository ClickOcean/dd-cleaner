using System.Text.Json.Serialization;

public class UsingMetrics
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public UsingMetricsAttributes Attributes { get; set; } = new();
}