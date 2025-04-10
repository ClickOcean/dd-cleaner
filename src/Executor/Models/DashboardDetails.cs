using System.Text.Json;
using System.Text.Json.Serialization;

public class DashboardDetails
{
    [JsonPropertyName("widgets")]
    public List<Widget> Widgets { get; set; } = [];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
