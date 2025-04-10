using System.Text.Json.Serialization;

public class DashboardsResponse
    {
        [JsonPropertyName("dashboards")]
        public List<Dashboard> Dashboards { get; set; }
    }
