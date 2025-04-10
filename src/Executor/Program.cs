using Executor.Config;
using System.Text;
using System.Text.Json;

class Program
{
    private static string DatadogApiUrlV1;
    private static string DatadogApiUrlV2;
    private static RateLimitedHttpClient _client;

    private static List<string> ExcludingTags = ["host", "cluster_name", "kube_cluster_name", "kube_node", "team"];

    static async Task Main(string[] args)
    {
        var configs = DataDogConfig.GetDataDogConfigs();
        if (configs.Length == 0)
        {
            Console.WriteLine("No credentials found");
            return;
        }

        foreach (var config in configs)
        {
            // Validate configuration before proceeding
            if (string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.AppKey))
            {
                Console.WriteLine($"Invalid configuration for {config.Organization}: API key or App key is missing");
                continue;
            }

            if (string.IsNullOrEmpty(config.ApiUrlV1) || string.IsNullOrEmpty(config.ApiUrlV2))
            {
                Console.WriteLine($"Invalid configuration for {config.Organization}: API URLs are missing");
                continue;
            }

            Console.WriteLine($"Using credentials for {config.Organization}");
            // Initialize the rate-limited client once
            _client = new RateLimitedHttpClient(config.ApiKey, config.AppKey);
            DatadogApiUrlV1 = config.ApiUrlV1;
            DatadogApiUrlV2 = config.ApiUrlV2;

            try
            {
                await ProcessMetrics();
            }
            finally
            {
                _client?.Dispose();
            }
        }
    }

    private static async Task ProcessMetrics()
    {
        try
        {
            List<string> allMetrics = await GetAllMetrics();
            var allMonitorsQueries = await GetQueriesFromMonitors();
            List<Dashboard> allDashboards = await GetAllDashboards();
            List<string> allDashboardQueries = await GetAllDashboardDetails(allDashboards);
            var allQueries = allMonitorsQueries.Concat(allDashboardQueries);
            var metricsTags = await GetAllUsingMetrics();

            foreach (var q in allQueries)
            {
                foreach (var metric in allMetrics)
                {
                    if (q != null && q.ToString().Contains(metric))
                    {
                        if (q.Contains("{*}"))
                        {
                            // Remove the metric from the list if it contains wildcard
                            metricsTags.Remove(metric);
                        }
                        else
                        {
                            foreach (var tag in metricsTags.TryGetValue(metric, out var tags) ? tags : [])
                            {
                                if (q.Contains(tag.Name))
                                {
                                    metricsTags[metric].Remove(tag);
                                    metricsTags[metric].Add((tag.Name, tag.Count + 1));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Set up only the tags that are used in the queries
            foreach (var metric in allMetrics)
            {
                await DeleteTagsConfiguration(metric);
                Console.WriteLine($"Deleted tags on metric: {metric}");
                var usedTags = (metricsTags.TryGetValue(metric, out var tags) ? tags : []).Where(x => x.Count > 0).Select(x => x.Name).ToList();
                await ExcludeTagsOnMetric(metric, ExcludingTags.Where(t => !usedTags.Contains(t)));
                Console.WriteLine($"Exclude tags on metric: {metric}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing metrics: {ex.Message}");
        }
    }

    private static async Task<List<string>> GetQueriesFromMonitors()
    {
        var response = await _client.GetStringAsync($"{DatadogApiUrlV1}/monitor");
        var monitorsResponse = JsonSerializer.Deserialize<List<DDMonitor>>(response);
        return [.. monitorsResponse?.Select(m => m.Query) ?? []];
    }

    private static async Task<List<string>> GetAllDashboardDetails(List<Dashboard> allDashboards)
    {
        var result = new List<string>();
        foreach (var dashboard in allDashboards)
        {
            await Console.Out.WriteLineAsync(dashboard.Id);
            result.AddRange(await GetDashboardDetails(dashboard.Id));
        }
        return result;
    }

    private static async Task<Dictionary<string, List<(string Name, int Count)>>> GetAllUsingMetrics()
    {
        var response = await _client.GetStringAsync($"{DatadogApiUrlV2}/metrics?filter[related_assets]=true&window[seconds]=2592000");// 2,630,000 seconds = 1 month
        var metricsResponse = JsonSerializer.Deserialize<UsingMetricsResponse>(response);

        return metricsResponse.Data.Where(x => x.Type == "manage_tags").ToDictionary(x => x.Id, x => x.Attributes.Tags.Select(y => (y, 1)).ToList());
    }

    private static async Task<List<string>> GetAllMetrics()
    {
        var seconds = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var response = await _client.GetStringAsync($"{DatadogApiUrlV1}/metrics?from={seconds}");
        var metricsResponse = JsonSerializer.Deserialize<MetricsResponse>(response);

        return metricsResponse.Metrics;
    }

    private static async Task<List<Dashboard>> GetAllDashboards()
    {
        var response = await _client.GetStringAsync($"{DatadogApiUrlV1}/dashboard");
        var dashboardsResponse = JsonSerializer.Deserialize<DashboardsResponse>(response);

        return dashboardsResponse.Dashboards;
    }

    private static async Task<List<string>> GetDashboardDetails(string dashboardId)
    {
        string response = await _client.GetStringAsync($"{DatadogApiUrlV1}/dashboard/{dashboardId}");
        var dashboardDetails = JsonSerializer.Deserialize<DashboardDetails>(response);
        return [.. dashboardDetails?.Widgets.SelectMany(x => x.Definition?.Requests?.SelectMany(y => y.Queries?.Select(z => z.Query) ?? []) ?? []) ?? []];
    }

    private static async Task DeleteTagsConfiguration(string metric)
    {
        var response = await _client.DeleteAsync($"{DatadogApiUrlV2}/metrics/{metric}/tags");
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine("{0}:{1}", response.StatusCode, body);
    }

    private static async Task<string> GetMetricTypeAsync(string metricName)
    {
        try
        {
            var response = await _client.GetStringAsync($"{DatadogApiUrlV1}/metrics/{metricName}");
            MetricResponse metricDetails = JsonSerializer.Deserialize<MetricResponse>(response) ?? throw new Exception("Failed to deserialize metric response");
            return metricDetails?.Type ?? string.Empty;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return string.Empty;
        }
    }

    private static async Task ExcludeTagsOnMetric(string metric, IEnumerable<string> tags)
    {
        var metricType = await GetMetricTypeAsync(metric);
        // Don't proceed with empty metric type
        if (string.IsNullOrEmpty(metricType))
        {
            Console.WriteLine($"Warning: Cannot exclude tags for {metric} - unable to determine metric type");
            return;
        }

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            data = new
            {
                type = "manage_tags",
                id = metric,
                attributes = new
                {
                    metric_type = metricType,
                    exclude_tags_mode = true,
                    tags
                }
            }
        }), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{DatadogApiUrlV2}/metrics/{metric}/tags", content);
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine("{0}:{1}", response.StatusCode, body);
    }
}