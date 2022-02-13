namespace ServiceSpy.HealthChecks;

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

    private readonly TimeSpan healthCheckInterval;

    private readonly HealthChecks.IHealthCheckExecutor healthChecker;
    private readonly IMetadataStore metadataStore;
    private readonly IMetadataHealthCheckStore metadataHealthCheckStore;
    private readonly INotificationSender? notificationSender;
    private readonly ILogger logger;

    private readonly List<Task<(ServiceMetadata, string)>> tasks = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="healthChecker">Health checker</param>
    /// <param name="metadataStore">Metadata store</param>
    /// <param name="metadataHealthCheckStore">Metadata health check store</param>
    /// <param name="notificationSender">Optionally send health check notifications</param>
    /// <param name="healthCheckInterval">How often to perform health checks</param>
    /// <param name="logger">Logger</param>
    public MetadataHealthChecker(HealthChecks.IHealthCheckExecutor healthChecker,
        IMetadataStore metadataStore,
        IMetadataHealthCheckStore metadataHealthCheckStore,
        INotificationSender? notificationSender,
        TimeSpan healthCheckInterval,
        ILogger<MetadataHealthChecker> logger)
    {
        this.healthChecker = healthChecker;
        this.metadataStore = metadataStore;
        this.metadataHealthCheckStore = metadataHealthCheckStore;
        this.notificationSender = notificationSender;
        this.healthCheckInterval = healthCheckInterval;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecks(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing metadata store health checks");
            }
            await Task.Delay(healthCheckInterval, stoppingToken);
        }
    }

    private async Task PerformHealthChecks(CancellationToken cancelToken)
    {
        tasks.Clear();

        // grab all the metadatas
        var metadatas = await metadataStore.GetMetadatasAsync(cancelToken: cancelToken);

        // perform health checks in parallel
        foreach (var metadata in metadatas)
        {
            tasks.Add(healthChecker.ExecuteAsync(metadata, cancelToken));
        }

        // wait for all health checks
        await Task.WhenAll(tasks);

        // take the results and modify the health check store
        var events = tasks.Select(t => (t.Result.Item1, t.Result.Item2));
        await metadataHealthCheckStore.SetHealthAsync(events, cancelToken);

        if (notificationSender is not null)
        {
            await notificationSender.SendMetadataAsync(events.Select(e => new MetadataNotification
            {
                HealthCheck = e.Item2,
                Metadata = e.Item1
            }), cancelToken);
        }
    }
}
