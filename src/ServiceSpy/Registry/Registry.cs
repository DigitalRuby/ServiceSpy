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
    /// <returns>Registry</returns>
    public static IRegistry Create(IEndPointStorage storage) => new Registry(storage);
}

/// <inheritdoc />
internal sealed class Registry : IRegistry
{
    private readonly IEndPointStorage storage;
    private readonly HashSet<INotificationHandler> handlers = new();

    public Registry(IEndPointStorage storage) : this(storage, Array.Empty<INotificationHandler>())
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">Storage</param>
    /// <param name="handlers">Handlers</param>
    public Registry(IEndPointStorage storage, IEnumerable<INotificationHandler> handlers)
    {
        this.storage = storage;
        foreach (var handler in handlers)
        {
            AddNotificationHandler(handler);
        }
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var results = await storage.UpsertAsync(request.Name, request.EndPoints);
        if (results is not null && results.Count != 0)
        {
            lock (handlers)
            {
                foreach (var handler in handlers)
                {
                    handler.SendEndPointChanged(new EndPointChangedEvent
                    {
                        Name = request.Name,
                        Changes = results
                    });
                }
            }
        }
        return new RegisterResponse
        {
            Changes = new Dictionary<EndPoint, EndPoint?>()
        };
    }

    /// <inheritdoc />
    public async Task<UnregisterResponse> UnregisterAsync(UnregisterRequest request)
    {
        (bool deleted, bool all) = await storage.DeleteAsync(request.Name, request.EndPoints);
        if (deleted)
        {
            lock (handlers)
            {
                foreach (var handler in handlers)
                {
                    handler.SendEndPointDeleted(new EndPointDeletedEvent
                    {
                        Name = request.Name,
                        EndPoints = request.EndPoints,
                        All = all
                    });
                }
            }
        }
        return new UnregisterResponse
        {
            All = all,
            Deleted = deleted
        };
    }

    /// <inheritdoc />
    public async Task<UnregisterAllResponse> UnregisterAllAsync(UnregisterAllRequest request)
    {
        var endPointsDeleted = await storage.DeleteAllAsync(request.Name);
        if (endPointsDeleted is not null)
        {
            lock (handlers)
            {
                foreach (var handler in handlers)
                {
                    handler.SendEndPointDeleted(new EndPointDeletedEvent
                    {
                        Name = request.Name,
                        EndPoints = endPointsDeleted,
                        All = true
                    });
                }
            }
        }
        return new UnregisterAllResponse
        {
            EndPoints = endPointsDeleted
        };
    }

    /// <inheritdoc />
    public Task<EndPoints?> GetEndpointsAsync(string name)
    {
        return storage.GetAsync(name);
    }

    /// <inheritdoc />
    public void AddNotificationHandler(INotificationHandler handler)
    {
        lock (handlers)
        {
            handlers.Add(handler);
            handler.ReceiveEndPointChanged += ReceiveEndPointChanged;
            handler.ReceiveEndPointDeleted += ReceiveEndPointDeleted;
        }
    }

    /// <inheritdoc />
    public bool RemoveNotificationHandler(INotificationHandler handler)
    {
        lock (handlers)
        {
            handler.ReceiveEndPointChanged -= ReceiveEndPointChanged;
            handler.ReceiveEndPointDeleted -= ReceiveEndPointDeleted;
            return handlers.Remove(handler);
        }
    }

    private void ReceiveEndPointDeleted(EndPointDeletedEvent obj)
    {
        
    }

    private void ReceiveEndPointChanged(EndPointChangedEvent obj)
    {
    }
}

