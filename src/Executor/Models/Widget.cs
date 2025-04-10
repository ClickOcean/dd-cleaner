using System.Text.Json.Serialization;

public class Widget
{
    [JsonPropertyName("definition")]
    public WidgetDefinition Definition { get; set; } = new();
}
