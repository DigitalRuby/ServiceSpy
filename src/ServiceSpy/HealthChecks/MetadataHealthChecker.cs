﻿namespace ServiceSpy.HealthChecks;

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
    private readonly ILogger logger;

    private readonly List<Task<(ServiceMetadata, string)>> tasks = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="healthChecker">Health checker</param>
    /// <param name="metadataStore">Metadata store</param>
    /// <param name="metadataHealthCheckStore">Metadata health check store</param>
    /// <param name="healthCheckInterval">How often to perform health checks</param>
    /// <param name="logger">Logger</param>
    public MetadataHealthChecker(HealthChecks.IHealthCheckExecutor healthChecker,
        IMetadataStore metadataStore,
        IMetadataHealthCheckStore metadataHealthCheckStore,
        TimeSpan healthCheckInterval,
        ILogger<MetadataHealthChecker> logger)
    {
        this.healthChecker = healthChecker;
        this.metadataStore = metadataStore;
        this.metadataHealthCheckStore = metadataHealthCheckStore;
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

        // grab all the metadatas
        var metadatas = await metadataStore.GetMetadatasAsync();

        // perform health checks in parallel
        foreach (var metadata in metadatas)
        {
            tasks.Add(healthChecker.ExecuteAsync(metadata));
        }

        // wait for all health checks
        await Task.WhenAll(tasks);

        // take the results and modify the health check store
        await metadataHealthCheckStore.SetHealthAsync(tasks.Select(t => (t.Result.Item1, t.Result.Item2)));
    }
}
