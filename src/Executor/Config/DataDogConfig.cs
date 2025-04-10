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
            var credentions = Environment.GetEnvironmentVariable("DATADOG_CREDENTIALS");
            var credentialsArray = JsonSerializer.Deserialize<DataDogCred[]>(credentions);

            var ApiUrlV1 = BaseUrl + "v1";
            var ApiUrlV2 = BaseUrl + "v2";

            if (credentialsArray == null || credentialsArray.Length == 0)
            {
                throw new Exception("No credentials found");
            }

            return [.. credentialsArray.Select(cred =>
            {
                return new DataDogConfig(BaseUrl, cred.AppKey, cred.ApiKey, cred.Organization);
            })];
        }
    }
}