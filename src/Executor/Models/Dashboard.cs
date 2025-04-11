using System.Text.Json.Serialization;

public class Dashboard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
