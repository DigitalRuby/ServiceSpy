namespace ServiceSpy.Storage;

/// <summary>
/// Stores end point info in memory
/// </summary>
public sealed class InMemoryEndPointStorage : IEndPointStorage
{
    private readonly Dictionary<string, EndPoints> allEndPoints = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<IReadOnlyCollection<EndPoint>?> DeleteAllAsync(string name)
    {
        lock (allEndPoints)
        {
            allEndPoints.Remove(name, out EndPoints? endPoints);
            return Task.FromResult<IReadOnlyCollection<EndPoint>?>(endPoints?.All.ToArray());
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string name, EndPoint endPoint)
    {
        lock (allEndPoints)
        {
            if (allEndPoints.TryGetValue(name, out EndPoints? endPoints))
            {
                bool removed = endPoints.Remove(endPoint, out bool empty);
                if (empty)
                {
                    allEndPoints.Remove(name);
                }
                return Task.FromResult<bool>(removed);
            }
        }
        return Task.FromResult<bool>(false);
    }

    /// <inheritdoc />
    public Task<EndPoints?> GetAsync(string name)
    {
        lock (allEndPoints)
        {
            if (allEndPoints.TryGetValue(name, out EndPoints? endPoints))
            {
                return Task.FromResult<EndPoints?>(endPoints);
            }
        }
        return Task.FromResult<EndPoints?>(null);
    }

    /// <inheritdoc />
    public Task<bool> UpsertAsync(string name, EndPoint endPoint, out EndPoint? oldEndPoint)
    {
        lock (allEndPoints)
        {
            if (!allEndPoints.TryGetValue(name, out EndPoints? endPoints))
            {
                allEndPoints[name] = endPoints = new(name);
            }
            bool change = endPoints.Upsert(endPoint, out oldEndPoint);
            return Task.FromResult<bool>(change);
        }
    }
}
