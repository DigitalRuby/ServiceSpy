namespace ServiceSpy.Registry;

/// <summary>
/// Stores metadata for services and performs health checks
/// </summary>
public class MetadataStore : BackgroundService, IDisposable
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

    private const int failuresToMoveToUnhealthy = 3;
    private static readonly TimeSpan healthCheckInterval = TimeSpan.FromSeconds(10.0);
    private static readonly TimeSpan healthCheckKickTimeSpan = TimeSpan.FromMinutes(10.0);

    private readonly object syncRoot = new();

    private readonly HealthChecks.IHealthCheckExecutor healthChecker;
    private readonly INotificationReceiver notificationReceiver;
    private readonly ILogger logger;

    private readonly Dictionary<ServiceMetadata, (ServiceMetadata, HealthCheckStatus)> healthyMetadatas = new();
    private readonly Dictionary<ServiceMetadata, (ServiceMetadata, HealthCheckStatus)> unhealthyMetadatas = new();
    private readonly List<Task<(ServiceMetadata, string)>> tasksHealthy = new();
    private readonly List<Task<(ServiceMetadata, string)>> tasksUnhealthy = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="healthChecker">Health checker</param>
    /// <param name="notificationReceiver">Notification receiver</param>
    /// <param name="logger">Logger</param>
    public MetadataStore(HealthChecks.IHealthCheckExecutor healthChecker,
        INotificationReceiver notificationReceiver,
        ILogger<MetadataStore> logger)
    {
        this.healthChecker = healthChecker;
        this.notificationReceiver = notificationReceiver;
        this.logger = logger;
        notificationReceiver.ReceiveMetadataAsync += ReceiveMetadataAsync;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        notificationReceiver.ReceiveMetadataAsync -= ReceiveMetadataAsync;
        base.Dispose();
    }

    /// <summary>
    /// Upsert service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <returns>Task</returns>
    public Task UpsertAsync(ServiceMetadata metadata)
    {
        lock (syncRoot)
        {
            // if unhealthy, update it in unhealthy dictionary
            if (unhealthyMetadatas.Remove(metadata, out (ServiceMetadata, HealthCheckStatus) status))
            {
                unhealthyMetadatas.Add(status.Item1, status);
            }
            else
            {
                // replace in healthy dictionary
                healthyMetadatas[metadata] = new(metadata, new());
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <returns>Task</returns>
    public Task RemoveAsync(ServiceMetadata metadata)
    {
        lock (syncRoot)
        {
            // remove from both stores
            healthyMetadatas.Remove(metadata);
            unhealthyMetadatas.Remove(metadata);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get healthy service metadatas
    /// </summary>
    /// <returns>Healthy metadatas</returns>
    public Task<IReadOnlyCollection<ServiceMetadata>> GetHealthyMetadatasAsync()
    {
        lock (syncRoot)
        {
            // make a copy of all healthy metadatas
            return Task.FromResult<IReadOnlyCollection<ServiceMetadata>>(healthyMetadatas.Keys.ToArray());
        }
    }

    /// <summary>
    /// Get unhealthy service metadatas
    /// </summary>
    /// <returns>Unhealthy metadatas</returns>
    public Task<IReadOnlyCollection<ServiceMetadata>> GetUnhealthyMetadatasAsync()
    {
        lock (syncRoot)
        {
            // make a copy of all unhealthy metadatas
            return Task.FromResult<IReadOnlyCollection<ServiceMetadata>>(unhealthyMetadatas.Keys.ToArray());
        }
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
        tasksHealthy.Clear();
        tasksUnhealthy.Clear();

        // get fresh copy of all metadatas
        var healthyMetadatas = await GetHealthyMetadatasAsync();
        var unhealthyMetadatas = await GetUnhealthyMetadatasAsync();

        // verify health of current healthy metadatas
        foreach (var metadata in healthyMetadatas)
        {
            tasksHealthy.Add(healthChecker.Execute(metadata));
        }

        // see if unhealthy metadatas are healthy again
        foreach (var metadata in unhealthyMetadatas)
        {
            tasksUnhealthy.Add(healthChecker.Execute(metadata));
        }
        
        // wait for all health checks
        await Task.WhenAll(tasksHealthy);
        await Task.WhenAll(tasksUnhealthy);

        // check for health changes
        foreach (var task in tasksHealthy)
        {
            // if we are healthy mark the healthy timestamp
            if (string.IsNullOrWhiteSpace(task.Result.Item2))
            {
                lock (syncRoot)
                {
                    if (this.healthyMetadatas.TryGetValue(task.Result.Item1, out (ServiceMetadata, HealthCheckStatus) status))
                    {
                        status.Item2.Clear();
                    }
                }
            }
            else
            {
                // move to unhealthy pool
                lock (syncRoot)
                {
                    if (this.healthyMetadatas.TryGetValue(task.Result.Item1, out (ServiceMetadata, HealthCheckStatus) status) &&
                        ++status.Item2.Failures > failuresToMoveToUnhealthy)
                    {
                        this.healthyMetadatas.Remove(task.Result.Item1);
                        this.unhealthyMetadatas[status.Item1] = status;
                    }
                }
            }
        }

        // check for unhealthy changes
        foreach (var task in tasksUnhealthy)
        {
            // if healthy again, move back to healthy pool
            if (string.IsNullOrWhiteSpace(task.Result.Item2))
            {
                lock (syncRoot)
                {
                    if (this.unhealthyMetadatas.TryGetValue(task.Result.Item1, out (ServiceMetadata, HealthCheckStatus) status))
                    {
                        status.Item2.Clear();
                        this.healthyMetadatas[status.Item1] = status;
                    }
                }
            }
            else
            {
                // if unhealthy for long enough, kick it out entirely
                lock (syncRoot)
                {
                    if (this.unhealthyMetadatas.TryGetValue(task.Result.Item1, out (ServiceMetadata, HealthCheckStatus) status))
                    {
                        if ((DateTimeOffset.UtcNow - status.Item2.LastHealthCheck) > healthCheckKickTimeSpan)
                        {
                            this.unhealthyMetadatas.Remove(task.Result.Item1);
                        }
                        else
                        {
                            status.Item2.Failures++;
                        }
                    }
                }
            }
        }
    }

    private Task ReceiveMetadataAsync(MetadataNotification evt, CancellationToken cancelToken)
    {
        lock (syncRoot)
        {
            if (evt.Deleted)
            {
                return RemoveAsync(evt.Metadata);
            }
            else
            {
                return UpsertAsync(evt.Metadata);
            }
        }
    }
}
