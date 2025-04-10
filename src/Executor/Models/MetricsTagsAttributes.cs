using System.Text.Json.Serialization;

public class MetricsTagsAttributes
{
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }
}
