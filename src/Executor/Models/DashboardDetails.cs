using System.Text.Json;
using System.Text.Json.Serialization;

public class DashboardDetails
    {
        // Define properties as per the response structure
        [JsonPropertyName("widgets")]
        public List<Widget> Widgets { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
