using System.Text.Json.Serialization;

public class UsingMetricsAttributes
{
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}