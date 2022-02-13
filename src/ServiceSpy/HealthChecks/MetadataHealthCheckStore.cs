namespace ServiceSpy.HealthChecks;

// TODO: The health check store is hard-coded to be in memory...
// Add an abstraction layer to be able to store health check results in different types of storage (redis, sql, etc.)

/// <summary>
/// Stores results of health checks for service metadata
/// </summary>
public interface IMetadataHealthCheckStore
{
    /// <summary>
    /// Set the health of a service metadata
    /// </summary>
    /// <param name="results">Health check results</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task</returns>
    Task SetHealthAsync(IEnumerable<(ServiceMetadata metadata, string error)> results, CancellationToken cancelToken = default);

    /// <summary>
    /// Get service metadata health
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task of string containing null if service metadata not found, empty string if healthy, otherwise a health check error</returns>
    Task<string?> GetHealthAsync(ServiceMetadata metadata, CancellationToken cancelToken = default);
}

/// <inheritdoc />
public sealed class MetadataHealthCheckStore : BackgroundService, IMetadataHealthCheckStore
{
    private const int maxFailuresBeforeUnhealthy = 3;

    private readonly object syncRoot = new();

    private readonly TimeSpan cleanupInterval;
    private readonly TimeSpan expireTimeSpan;
    private readonly ILogger logger;

    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> healthyMetadatas = new();
    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> unhealthyMetadatas = new();
    private readonly List<ServiceMetadata> removals = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="cleanupInterval">Cleanup interval, how often to check for metadatas that are expired</param>
    /// <param name="expireTimeSpan">TimeSpan after which health checks that haven't reported back will be removed</param>
    /// <param name="logger">Logger</param>
    public MetadataHealthCheckStore(TimeSpan cleanupInterval,
        TimeSpan expireTimeSpan,
        ILogger<MetadataHealthCheckStore> logger)
    {
        this.cleanupInterval = cleanupInterval;
        this.expireTimeSpan = expireTimeSpan;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task SetHealthAsync(IEnumerable<(ServiceMetadata metadata, string error)> results, CancellationToken cancelToken = default)
    {
        lock (syncRoot)
        {
            foreach (var item in results)
            {
                var metadata = item.metadata;
                var error = item.error;

                // if we are healthy mark the healthy timestamp
                if (string.IsNullOrWhiteSpace(error))
                {
                    if (unhealthyMetadatas.Remove(metadata, out HealthCheckStatus? status))
                    {
                        // reset status, recovered
                        status.Clear();

                        // put back in healthy pool
                        healthyMetadatas[metadata] = status;

                        // TODO: Notify of change in status
                    }
                    else if (healthyMetadatas.TryGetValue(metadata, out status))
                    {
                        // already in healthy pool, clear status
                        status.Clear();
                    }
                    else
                    {
                        // not in either pool, add to healthy pool
                        healthyMetadatas[metadata] = new();
                    }
                }
                else if (error == "X")
                {
                    // nuke
                    if (healthyMetadatas.Remove(metadata))
                    {
                        // TODO: Notify of status change
                    }

                    if (unhealthyMetadatas.Remove(metadata))
                    {
                        // TODO: Notify of status change
                    }
                }
                else
                {
                    // move to unhealthy pool if needed
                    if (!healthyMetadatas.TryGetValue(metadata, out HealthCheckStatus? status))
                    {
                        // already in unhealthy pool
                        if (unhealthyMetadatas.TryGetValue(metadata, out status))
                        {
                            status.Fail(error);
                        }
                        else
                        {
                            // new failed health check entry, still healthy until more failures
                            healthyMetadatas[metadata] = new() { Failures = 1, LastError = error };

                            // TODO: Notify of status change
                        }
                    }
                    else if (++status.Failures > maxFailuresBeforeUnhealthy)
                    {
                        // move from healthy to unhealthy
                        healthyMetadatas.Remove(metadata);
                        status.LastError = error;
                        status.LastHealthCheck = DateTimeOffset.UtcNow;
                        unhealthyMetadatas[metadata] = status;

                        // TODO: Notify of status change
                    }
                    else
                    {
                        // update last check only
                        status.LastHealthCheck = DateTimeOffset.UtcNow;
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetHealthAsync(ServiceMetadata metadata, CancellationToken cancelToken = default)
    {
        lock (syncRoot)
        {
            if (healthyMetadatas.ContainsKey(metadata))
            {
                return Task.FromResult<string?>(string.Empty);
            }
            else if (unhealthyMetadatas.TryGetValue(metadata, out HealthCheckStatus? status))
            {
                return Task.FromResult<string?>(status.LastError);
            }
        }
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RemoveDeadHealthChecks();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing metadata health check store");
            }
            await Task.Delay(cleanupInterval, stoppingToken);
        }
    }

    private void RemoveDeadHealthChecks()
    {
        lock (syncRoot)
        {
            foreach (var kv in healthyMetadatas)
            {
                if ((DateTimeOffset.UtcNow - kv.Value.LastHealthCheck) > expireTimeSpan)
                {
                    removals.Add(kv.Key);
                }
            }
            foreach (var kv in unhealthyMetadatas)
            {
                if ((DateTimeOffset.UtcNow - kv.Value.LastHealthCheck) > expireTimeSpan)
                {
                    removals.Add(kv.Key);
                }
            }
            foreach (var metadata in removals)
            {
                if (healthyMetadatas.Remove(metadata))
                {
                    // TODO: Notify of status change
                }

                if (unhealthyMetadatas.Remove(metadata))
                {
                    // TODO: Notify of status change
                }
            }
        }
    }
}
