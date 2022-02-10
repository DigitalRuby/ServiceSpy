namespace ServiceSpy.Registry;

/// <summary>
/// Metadata storage interface
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Retrieve all metadatas in the storage
    /// </summary>
    /// <returns>Metadatas</returns>
    Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync();

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
    private readonly Dictionary<ServiceMetadata, ServiceMetadata> metadatas = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="notificationReceiver">Notification receiver</param>
    public MetadataStore(INotificationReceiver notificationReceiver)
    {
        this.notificationReceiver = notificationReceiver;
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
    public Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<ServiceMetadata>>(metadatas.Keys.ToArray());
        }
    }

    private Task ReceiveMetadataAsync(MetadataNotification evt, CancellationToken cancelToken)
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
