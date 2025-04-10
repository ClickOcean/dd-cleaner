using System.Text.Json.Serialization;

public class WithQuery
{
    [JsonPropertyName("query")]
    public string Query { get; set; }
}