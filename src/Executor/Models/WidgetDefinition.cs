using System.Text.Json.Serialization;

public class WidgetDefinition
{
    [JsonPropertyName("requests")]
    public List<WidgetRequest> Requests { get; set; } = [];
}
