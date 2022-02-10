namespace ServiceSpy.Registry;

// TODO: The metadata store is hard-coded to be in memory
// We will want an abstraction layer to be able to store metadata in different types of storage (redis, sql, etc.)

/// <summary>
/// Metadata storage interface
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Retrieve all metadatas in the storage
    /// </summary>
    /// <param name="serviceId">Service id to get metadatas for or null for all</param>
    /// <returns>Metadatas</returns>
    Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync(Guid? serviceId = null);

    /// <summary>
    /// Upsert service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <returns>Task</returns>
    Task UpsertAsync(ServiceMetadata metadata);

    /// <summary>
    /// Remove service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <returns>Task</returns>
    Task RemoveAsync(ServiceMetadata metadata);
}

/// <summary>
/// Stores metadata for services and performs health checks
/// </summary>
public sealed class MetadataStore : IDisposable
{
    private readonly object syncRoot = new();

    private readonly INotificationReceiver notificationReceiver;
    private readonly HealthChecks.IMetadataHealthCheckStore healthCheckStore;

    private readonly Dictionary<ServiceMetadata, ServiceMetadata> metadatas = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="notificationReceiver">Notification receiver</param>
    /// <param name="healthCheckStore">Health check store</param>
    public MetadataStore(INotificationReceiver notificationReceiver, HealthChecks.IMetadataHealthCheckStore healthCheckStore)
    {
        this.notificationReceiver = notificationReceiver;
        this.healthCheckStore = healthCheckStore;
        notificationReceiver.ReceiveMetadataAsync += ReceiveMetadataAsync;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        notificationReceiver.ReceiveMetadataAsync -= ReceiveMetadataAsync;
    }

    /// <inheritdoc />
    public Task UpsertAsync(ServiceMetadata metadata)
    {
        lock (syncRoot)
        {
            metadatas[metadata] = metadata;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(ServiceMetadata metadata)
    {
        lock (syncRoot)
        {
            metadatas.Remove(metadata);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync(Guid? serviceId = null)
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<ServiceMetadata>>(metadatas.Keys.Where(k => serviceId is null || k.Id == serviceId).ToArray());
        }
    }

    private async Task ReceiveMetadataAsync(MetadataNotification evt, CancellationToken cancelToken)
    {
        // if we have health check info, pass it on
        if (evt.HealthCheck is not null)
        {
            await healthCheckStore.SetHealthAsync(evt.Metadata, evt.HealthCheck);
        }

        if (evt.Deleted)
        {
            await RemoveAsync(evt.Metadata);
        }
        else
        {
            await UpsertAsync(evt.Metadata);
        }
    }
}
