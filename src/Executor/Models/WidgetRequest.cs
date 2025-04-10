using System.Text.Json.Serialization;

public class WidgetRequest
{
    [JsonPropertyName("queries")]
    public List<WithQuery> Queries { get; set; }
}
