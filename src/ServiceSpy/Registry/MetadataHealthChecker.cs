namespace ServiceSpy.Registry;

/// <summary>
/// Performs health checks and stores status for metadata
/// </summary>
public interface IMetadataHealthChecker
{
}

/// <inheritdoc />
public sealed class MetadataHealthChecker : BackgroundService, IMetadataHealthChecker
{
    private sealed class HealthCheckStatus
    {
        public void Clear()
        {
            LastHealthCheck = DateTimeOffset.UtcNow;
            LastError = string.Empty;
            Failures = 0;
        }

        public DateTimeOffset LastHealthCheck { get; set; } = DateTimeOffset.UtcNow;

        public string LastError { get; set; } = string.Empty;

        public int Failures { get; set; }
    }

    private const int maxFailuresBeforeUnhealthy = 3;
    private static readonly TimeSpan healthCheckInterval = TimeSpan.FromSeconds(10.0);
    private static readonly TimeSpan healthCheckKickTimeSpan = TimeSpan.FromMinutes(10.0);

    private readonly object syncRoot = new();
    private readonly HealthChecks.IHealthCheckExecutor healthChecker;
    private readonly IMetadataStore metadataStore;
    private readonly ILogger logger;

    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> healthyMetadatas = new();
    private readonly Dictionary<ServiceMetadata, HealthCheckStatus> unhealthyMetadatas = new();

    private readonly List<Task<(ServiceMetadata, string)>> tasks = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="healthChecker">Health checker</param>
    /// <param name="metadataStore">Metadata store</param>
    /// <param name="logger">Logger</param>
    public MetadataHealthChecker(HealthChecks.IHealthCheckExecutor healthChecker,
        IMetadataStore metadataStore,
        ILogger<MetadataStore> logger)
    {
        this.healthChecker = healthChecker;
        this.metadataStore = metadataStore;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecks();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing metadata store health checks");
            }
            await Task.Delay(healthCheckInterval, stoppingToken);
        }
    }

    private async Task PerformHealthChecks()
    {
        tasks.Clear();

        var metadatas = await metadataStore.GetMetadatasAsync();
        foreach (var metadata in metadatas)
        {
            tasks.Add(healthChecker.Execute(metadata));
        }

        // wait for all health checks
        await Task.WhenAll(tasks);

        lock (syncRoot)
        {
            // check for health changes
            foreach (var task in tasks)
            {
                // if we are healthy mark the healthy timestamp
                if (string.IsNullOrWhiteSpace(task.Result.Item2))
                {
                    if (unhealthyMetadatas.Remove(task.Result.Item1, out HealthCheckStatus? status))
                    {
                        // reset status
                        status.Clear();

                        // put back in healthy pool
                        healthyMetadatas[task.Result.Item1] = status;

                        // TODO: Notify of change in status
                    }
                    else if (healthyMetadatas.TryGetValue(task.Result.Item1, out status))
                    {
                        // already in healthy pool, clear status
                        status.Clear();
                    }
                    else
                    {
                        // not in either pool, add to healthy pool
                        healthyMetadatas[task.Result.Item1] = new();
                    }
                }
                else
                {
                    // move to unhealthy pool if needed
                    if (!healthyMetadatas.TryGetValue(task.Result.Item1, out HealthCheckStatus? status))
                    {
                        // if already in unhealthy pool, see if we need to kick it out entirely
                        if (unhealthyMetadatas.TryGetValue(task.Result.Item1, out status))
                        {
                            status.Failures++;
                            status.LastError = task.Result.Item2;

                            // boot if enough time of failures has elapsed
                            if ((DateTimeOffset.UtcNow - status.LastHealthCheck) > healthCheckKickTimeSpan)
                            {
                                unhealthyMetadatas.Remove(task.Result.Item1);

                                // TODO: Notify of status change
                            }
                        }
                        else
                        {
                            // new failed health check entry, still healthy until more failures
                            healthyMetadatas[task.Result.Item1] = new() { Failures = 1, LastError = task.Result.Item2 };
                        }
                    }
                    else if (++status.Failures > maxFailuresBeforeUnhealthy)
                    {
                        // move from healthy to unhealthy
                        healthyMetadatas.Remove(task.Result.Item1);
                        status.LastError = task.Result.Item2;
                        unhealthyMetadatas[task.Result.Item1] = status;

                        // TODO: Notify of status change
                    }
                }
            }
        }
    }
}
