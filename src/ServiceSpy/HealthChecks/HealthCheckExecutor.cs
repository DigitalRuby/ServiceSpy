namespace ServiceSpy.HealthChecks;

/// <summary>
/// Executes health checks against end points and flags any unhealth end points
/// </summary>
public interface IHealthCheckExecutor
{
    /// <summary>
    /// Execute a health check
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task of string, metadata, empty string if success, otherwise an error string of why the health check failed</returns>
    Task<(ServiceMetadata, string)> ExecuteAsync(ServiceMetadata metadata, CancellationToken cancelToken = default);
}

/// <inheritdoc />
public class HealthCheckExecutor : IHealthCheckExecutor
{
    /// <summary>
    /// Health check http client key
    /// </summary>
    public const string HealthCheckExecutorKey = "HealthCheckClient";

    private readonly HttpClient client;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClientFactory">Http client factory, uses HealthCheckClient key</param>
    public HealthCheckExecutor(IHttpClientFactory httpClientFactory)
    {
        client = httpClientFactory.CreateClient(HealthCheckExecutorKey);
    }

    /// <inheritdoc />
    public async Task<(ServiceMetadata, string)> ExecuteAsync(ServiceMetadata metadata, CancellationToken cancelToken)
    {
        try
        {
            var url = (metadata.Port == 80 ? "http://" : "https://") + metadata.Host + metadata.HealthCheckPath +
                (metadata.Port != 80 && metadata.Port != 443 ? ":" + metadata.Port : string.Empty);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await client.SendAsync(msg, cancelToken);
            if (result.IsSuccessStatusCode)
            {
                return (metadata, string.Empty);
            }
            string error = await result.Content.ReadAsStringAsync(cancelToken);
            if (string.IsNullOrWhiteSpace(error))
            {
                error = result.StatusCode.ToString();
            }
            return (metadata, error);
        }
        catch (Exception ex)
        {
            return (metadata, ex.Message);
        }
    }
}
