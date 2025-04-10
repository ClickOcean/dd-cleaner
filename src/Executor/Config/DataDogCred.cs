using System.Diagnostics;
using System.Text.Json.Serialization;

public class DataDogCred
{
    [JsonPropertyName("appKey")]
    [DebuggerDisplay("{GetMaskedValue(AppKey)}")]
    public string AppKey { get; set; }

    [JsonPropertyName("apiKey")]
    [DebuggerDisplay("{GetMaskedValue(ApiKey)}")]
    public string ApiKey { get; set; }

    [JsonPropertyName("org")]
    public string Organization { get; set; }

    private static string GetMaskedValue(string value) => string.IsNullOrEmpty(value) ? "<empty>" : $"{value[..4]}***";
}