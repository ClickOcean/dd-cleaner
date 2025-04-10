using System.Text.Json;

namespace Executor.Config
{
    public record DataDogConfig(string BaseUrl, string AppKey, string ApiKey, string Organization)
    {
        public string ApiUrlV1 => BaseUrl + "v1";
        public string ApiUrlV2 => BaseUrl + "v2";

        public static DataDogConfig[] GetDataDogConfigs()
        {
            var BaseUrl = Environment.GetEnvironmentVariable("DATADOG_BASE_URL") ?? "https://api.datadoghq.com/api/";
            var credentials = Environment.GetEnvironmentVariable("DATADOG_CREDENTIALS");
            if (string.IsNullOrEmpty(credentials))
            {
                throw new Exception("DATADOG_CREDENTIALS environment variable is not set");
            }

            var credentialsArray = JsonSerializer.Deserialize<DataDogCred[]>(credentials);

            if (string.IsNullOrEmpty(credentials))
            {
                throw new Exception("DATADOG_CREDENTIALS environment variable is not set");
            }

            try
            {
                credentialsArray = JsonSerializer.Deserialize<DataDogCred[]>(credentials) ?? [];
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse DATADOG_CREDENTIALS as JSON", ex);
            }

            return [.. credentialsArray.Select(cred =>
            {
                return new DataDogConfig(BaseUrl, cred.AppKey, cred.ApiKey, cred.Organization);
            })];
        }
    }
}