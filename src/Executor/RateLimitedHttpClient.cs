public class RateLimitedHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _throttler;
    private TimeSpan _throttleDuration;
    private readonly string _apiKey;
    private readonly string _appKey;
    private bool _disposed;

    public RateLimitedHttpClient(string apiKey, string appKey, int requestsPerSecond = 5, TimeSpan? throttleDuration = null)
    {
        _apiKey = apiKey;
        _appKey = appKey;
        _throttler = new SemaphoreSlim(requestsPerSecond);
        _throttleDuration = throttleDuration ?? TimeSpan.FromSeconds(1);
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("DD-API-KEY", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("DD-APPLICATION-KEY", _appKey);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        await _throttler.WaitAsync(cancellationToken);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Handle rate limiting (429 Too Many Requests)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                // Check for X-RateLimit-Period header
                if (response.Headers.TryGetValues("x-ratelimit-period", out var values) &&
                    int.TryParse(values.FirstOrDefault(), out var retryAfterSeconds))
                {
                    Console.WriteLine($"Rate limited according with header. Waiting for {retryAfterSeconds} seconds before retry.");
                    await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), cancellationToken);
                }
                else
                {
                    // Default backoff if no X-RateLimit-Period header
                    Console.WriteLine($"Rate limited. Using default backoff of {_throttleDuration} second.");
                    await Task.Delay(TimeSpan.FromSeconds(_throttleDuration.TotalSeconds), cancellationToken);
                    _throttleDuration = TimeSpan.FromSeconds(_throttleDuration.TotalSeconds * 2); // Exponential backoff
                }

                // Retry the request
                request = await CloneHttpRequestMessageAsync(request);
                return await SendAsync(request, cancellationToken);
            }

            if (_throttleDuration.TotalSeconds > 1)
            {
                // Reset throttle duration if we are not rate limited
                _throttleDuration = TimeSpan.FromSeconds(1);
            }

            return response;
        }
        finally
        {
            // Release after throttle duration to maintain the rate limit
            _ = Task.Delay(_throttleDuration).ContinueWith(_ => _throttler.Release());
        }
    }

    public async Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        return await SendAsync(request, cancellationToken);
    }

    // Helper to clone a request for retries
    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copy content if present
        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // Copy content headers
            if (request.Content.Headers != null)
                foreach (var h in request.Content.Headers)
                    clone.Content.Headers.Add(h.Key, h.Value);
        }

        // Copy request headers
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        // Copy properties
        foreach (var prop in request.Properties)
            clone.Properties.Add(prop);

        return clone;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _httpClient?.Dispose();
            _throttler?.Dispose();
        }

        _disposed = true;
    }
}
