namespace ServiceSpy.Registry;

/// <summary>
/// Registry factory
/// </summary>
public static class RegistryFactory
{
    /// <summary>
    /// Create a service registry
    /// </summary>
    /// <param name="storage">Storage</param>
    /// <param name="handler">Receives change notifications</param>
    /// <returns>Registry</returns>
    public static IRegistry Create(IEndPointStorage storage, INotificationReceiver handler) => new Registry(storage, handler);
}

/// <inheritdoc />
internal sealed class Registry : IRegistry, IDisposable
{
    private readonly IEndPointStorage storage;
    private readonly INotificationReceiver handler;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">Storage</param>
    /// <param name="handler">Provides change notifications</param>
    public Registry(IEndPointStorage storage, INotificationReceiver handler)
    {
        this.storage = storage;
        this.handler = handler;
        handler.ReceiveEndPointChangedAsync += ReceiveEndPointChangedAsync;
        handler.ReceiveEndPointDeletedAsync += ReceiveEndPointDeletedAsync;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        handler.ReceiveEndPointChangedAsync -= ReceiveEndPointChangedAsync;
        handler.ReceiveEndPointDeletedAsync -= ReceiveEndPointDeletedAsync;
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var results = await storage.UpsertAsync(request.Id, request.EndPoints);
        return new RegisterResponse
        {
            Changes = results
        };
    }

    /// <inheritdoc />
    public async Task<UnregisterResponse> UnregisterAsync(UnregisterRequest request)
    {
        (bool deleted, bool all) = await storage.DeleteAsync(request.Id, request.EndPoints);
        return new UnregisterResponse
        {
            All = all,
            Deleted = deleted
        };
    }

    /// <inheritdoc />
    public async Task<UnregisterAllResponse> UnregisterAllAsync(UnregisterAllRequest request)
    {
        var endPointsDeleted = await storage.DeleteAllAsync(request.Id);
        return new UnregisterAllResponse
        {
            EndPoints = endPointsDeleted
        };
    }

    /// <inheritdoc />
    public Task<EndPoints?> GetEndpointsAsync(Guid id)
    {
        return storage.GetAsync(id);
    }


    private Task ReceiveEndPointChangedAsync(EndPointChangedEvent obj)
    {
        return storage.UpsertAsync(obj.Id, obj.Changes.Keys);
    }

    private Task ReceiveEndPointDeletedAsync(EndPointDeletedEvent obj)
    {
        return storage.DeleteAsync(obj.Id, obj.EndPoints);
    }
}

