namespace ServiceSpy.Registry;

/// <summary>
/// Metadata storage interface
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Retrieve all metadatas in the storage
    /// </summary>
    /// <param name="serviceId">Service id to get metadatas for or null for all</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Metadatas</returns>
    Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync(Guid? serviceId = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Retrieve a set of healthy metadatas that can be used to attempt api calls
    /// </summary>
    /// <param name="cache">Whether to allow cached data</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Metadatas</returns>
    Task<IReadOnlyCollection<ServiceMetadata>> GetHealthyMetadatasAsync(bool cache = true, CancellationToken cancelToken = default);

    /// <summary>
    /// Upsert service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task</returns>
    Task UpsertAsync(ServiceMetadata metadata, CancellationToken cancelToken = default);

    /// <summary>
    /// Remove service metadata
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task of bool that specifies if metadata was removed</returns>
    Task<bool> RemoveAsync(ServiceMetadata metadata, CancellationToken cancelToken = default);
}

/// <summary>
/// Stores metadata for services and performs health checks
/// </summary>
public sealed class MetadataStore : IMetadataStore, IDisposable
{
    private readonly object syncRoot = new();

    private readonly INotificationReceiver notificationReceiver;
    private readonly HealthChecks.IMetadataHealthCheckStore healthCheckStore;
    private readonly TimeSpan healthyMetadataCacheTime;

    private readonly Dictionary<ServiceMetadata, ServiceMetadata> metadatas = new();

    private List<ServiceMetadata> healthyMetadatasCache = new();
    private DateTimeOffset lastHealthyMetadatasCacheTime;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="notificationReceiver">Notification receiver</param>
    /// <param name="healthCheckStore">Health check store</param>
    /// <param name="healthyMetadataCacheTime">Amount of time to cache healthy metadatas, null for 5 seconds</param>
    public MetadataStore(INotificationReceiver notificationReceiver,
        HealthChecks.IMetadataHealthCheckStore healthCheckStore,
        TimeSpan? healthyMetadataCacheTime = default)
    {
        this.notificationReceiver = notificationReceiver;
        this.healthCheckStore = healthCheckStore;
        this.healthyMetadataCacheTime = healthyMetadataCacheTime is not null ? healthyMetadataCacheTime.Value : TimeSpan.FromSeconds(5.0);
        notificationReceiver.ReceiveMetadataAsync += ReceiveMetadataAsync;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        notificationReceiver.ReceiveMetadataAsync -= ReceiveMetadataAsync;
    }

    /// <inheritdoc />
    public Task UpsertAsync(ServiceMetadata metadata, CancellationToken cancelToken = default)
    {
        lock (syncRoot)
        {
            metadatas[metadata] = metadata;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(ServiceMetadata metadata, CancellationToken cancelToken = default)
    {
        lock (syncRoot)
        {
            return Task.FromResult<bool>(metadatas.Remove(metadata));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<ServiceMetadata>> GetMetadatasAsync(Guid? serviceId = null, CancellationToken cancelToken = default)
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<ServiceMetadata>>(metadatas.Keys.Where(k => serviceId is null || k.Id == serviceId).ToArray());
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ServiceMetadata>> GetHealthyMetadatasAsync(bool cache = true, CancellationToken cancelToken = default)
    {
        static async Task AddHealthyMetadatas(List<ServiceMetadata> result, IEnumerable<ServiceMetadata> source,
            int maxCount, HealthChecks.IMetadataHealthCheckStore healthCheckStore, CancellationToken cancelToken)
        {
            int count = 0;
            foreach (var item in source)
            {
                if (await healthCheckStore.GetHealthAsync(item, cancelToken) == string.Empty)
                {
                    result.Add(item);
                    if (++count >= maxCount)
                    {
                        break;
                    }
                }
            }
        }

        // check if we have cached data
        if (cache && (DateTimeOffset.UtcNow - lastHealthyMetadatasCacheTime) < healthyMetadataCacheTime)
        {
            return healthyMetadatasCache;
        }

        List<ServiceMetadata> result = new();
        IGrouping<string, ServiceMetadata>[] grouping;
        lock (syncRoot)
        {
            grouping = metadatas.Keys.GroupBy(x => x.Group).ToArray();
        }
        var groupCount = grouping.Length;
        foreach (var group in grouping)
        {
            if (groupCount < 2)
            {
                // one group - three
                await AddHealthyMetadatas(result, group, 3, healthCheckStore, cancelToken);
            }
            else if (groupCount < 3)
            {
                // two groups - two
                await AddHealthyMetadatas(result, group, 2, healthCheckStore, cancelToken);
            }
            else
            {
                // three+ groups - one
                await AddHealthyMetadatas(result, group, 1, healthCheckStore, cancelToken);
            }
        }

        // only cache if something healthy was returned
        if (cache && result.Count != 0)
        {
            healthyMetadatasCache = result;
            lastHealthyMetadatasCacheTime = DateTimeOffset.UtcNow;
        }

        return result;
    }

    private async Task ReceiveMetadataAsync(MetadataNotification evt, CancellationToken cancelToken)
    {
        // if we have health check info, pass it on
        if (evt.HealthCheck is not null)
        {
            await healthCheckStore.SetHealthAsync(new[] { (evt.Metadata, evt.HealthCheck) }, cancelToken);
        }

        if (evt.Deleted)
        {
            await RemoveAsync(evt.Metadata, cancelToken);
        }
        else
        {
            await UpsertAsync(evt.Metadata, cancelToken);
        }
    }
}
