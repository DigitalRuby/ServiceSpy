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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">Storage</param>
    public Registry(IEndPointStorage storage)
    {
        this.storage = storage;
    }

    /// <inheritdoc />
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        bool change = await storage.UpsertAsync(request.Name, request.EndPoint, out EndPoint? oldEndPoint);
        if (change)
        {
            EndPointChanged?.Invoke(new EndPointChangedEvent
            {
                Name = request.Name,
                OldEndPoint = oldEndPoint,
                EndPoint = request.EndPoint
            });
        }
        return new RegisterResponse
        {
            Change = change,
            OldEndPoint = oldEndPoint
        };
    }

    /// <inheritdoc />
    public async Task<UnregisterResponse> UnregisterAsync(UnregisterRequest request)
    {
        bool deleted = await storage.DeleteAsync(request.Name, request.EndPoint);
        if (deleted)
        {
            EndPointDeleted?.Invoke(new EndPointDeletedEvent
            {
                Name = request.Name,
                EndPoint = request.EndPoint
            });
        }
        return new UnregisterResponse { Deleted = deleted };
    }

    /// <inheritdoc />
    public async Task<UnregisterAllResponse> UnregisterAllAsync(UnregisterAllRequest request)
    {
        var endPointsDeleted = await storage.DeleteAllAsync(request.Name);
        if (endPointsDeleted is not null)
        {
            foreach (var endPoint in endPointsDeleted)
            {
                EndPointDeleted?.Invoke(new EndPointDeletedEvent
                {
                    Name = request.Name, EndPoint = endPoint
                });
            }
        }
        return new UnregisterAllResponse { EndPoints = endPointsDeleted };
    }

    /// <inheritdoc />
    public Task<EndPoints?> GetEndpointsAsync(string name)
    {
        return storage.GetAsync(name);
    }

    /// <inheritdoc />
    public event Action<EndPointChangedEvent>? EndPointChanged;

    /// <inheritdoc />
    public event Action<EndPointDeletedEvent>? EndPointDeleted;
}

