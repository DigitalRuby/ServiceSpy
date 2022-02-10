namespace ServiceSpy.Registry;

// TODO: The health check store is hard-coded to be in memory
// We will want an abstraction layer to be able to store health check results in different types of storage (redis, sql, etc.)

/// <summary>
/// Stores results of health checks for service metadata
/// </summary>
public interface IMetadataHealthCheckStore
{
    /// <summary>
    /// Set the health of a service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="error">Error, empty string for healthy</param>
    /// <returns>Task</returns>
    Task SetHealthAsync(ServiceMetadata metadata, string error);
}

/// <inheritdoc />
public sealed class MetadataHealthCheckStore : BackgroundService, IMetadataHealthCheckStore
{
    private const int maxFailuresBeforeUnhealthy = 3;
    private static readonly TimeSpan healthCheckInterval = TimeSpan.FromSeconds(10.0);
    private static readonly TimeSpan healthCheckDeadTimeSpan = TimeSpan.FromMinutes(1.0);

    private readonly object syncRoot = new();
    private readonly ILogger logger;

    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> healthyMetadatas = new();
    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> unhealthyMetadatas = new();
    private readonly List<ServiceMetadata> removals = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public MetadataHealthCheckStore(ILogger<MetadataStore> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task SetHealthAsync(ServiceMetadata metadata, string error)
    {
        lock (syncRoot)
        {
            // if we are healthy mark the healthy timestamp
            if (string.IsNullOrWhiteSpace(error))
            {
                if (unhealthyMetadatas.Remove(metadata, out HealthCheckStatus? status))
                {
                    // reset status
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
            else
            {
                // move to unhealthy pool if needed
                if (!healthyMetadatas.TryGetValue(metadata, out HealthCheckStatus? status))
                {
                    // if already in unhealthy pool, see if we need to kick it out entirely
                    if (unhealthyMetadatas.TryGetValue(metadata, out status))
                    {
                        status.Failures++;
                        status.LastError = error;
                    }
                    else
                    {
                        // new failed health check entry, still healthy until more failures
                        healthyMetadatas[metadata] = new() { Failures = 1, LastError = error };
                    }
                }
                else if (++status.Failures > maxFailuresBeforeUnhealthy)
                {
                    // move from healthy to unhealthy
                    healthyMetadatas.Remove(metadata);
                    status.LastError = error;
                    unhealthyMetadatas[metadata] = status;

                    // TODO: Notify of status change
                }
            }
        }

        return Task.CompletedTask;
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
            await Task.Delay(healthCheckInterval, stoppingToken);
        }
    }

    private void RemoveDeadHealthChecks()
    {
        lock (syncRoot)
        {
            foreach (var kv in healthyMetadatas)
            {
                if ((DateTimeOffset.UtcNow - kv.Value.LastHealthCheck) > healthCheckDeadTimeSpan)
                {
                    removals.Add(kv.Key);
                }
            }
            foreach (var kv in unhealthyMetadatas)
            {
                if ((DateTimeOffset.UtcNow - kv.Value.LastHealthCheck) > healthCheckDeadTimeSpan)
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
