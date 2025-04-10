using System.Text.Json.Serialization;

public class MetricsTagsData
{
    [JsonPropertyName("attributes")]
    public MetricsTagsAttributes Attributes { get; set; }
}
