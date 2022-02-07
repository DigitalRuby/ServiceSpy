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
            return Task.FromResult<IReadOnlyCollection<EndPoint>?>(endPoints?.GetAllEndPoints());
        }
    }

    /// <inheritdoc />
    public Task<(bool, bool)> DeleteAsync(string name, IReadOnlyCollection<EndPoint> endPoints)
    {
        lock (allEndPoints)
        {
            bool removed = false;
            bool empty = false;
            if (this.allEndPoints.TryGetValue(name, out EndPoints? allEndPoints))
            {
                foreach (var endPoint in endPoints)
                {
                    removed |= allEndPoints.Remove(endPoint, out bool _empty);
                    empty |= _empty;
                }
                if (empty)
                {
                    this.allEndPoints.Remove(name);
                }
            }
            return Task.FromResult<(bool, bool)>((removed, empty));
        }
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
    public Task<IReadOnlyDictionary<EndPoint, EndPoint?>> UpsertAsync(string name, IReadOnlyCollection<EndPoint> endPoints)
    {
        lock (allEndPoints)
        {
            // make end points if we need to
            if (!allEndPoints.TryGetValue(name, out EndPoints? currentEndPoints))
            {
                allEndPoints[name] = currentEndPoints = new(name);
            }

            // results
            var results = new Dictionary<EndPoint, EndPoint?>();

            // upsert each end point
            foreach (var endPoint in endPoints)
            {
                bool change = currentEndPoints.Upsert(endPoint, out EndPoint? oldEndPoint);
                if (change)
                {
                    results[endPoint] = oldEndPoint;
                }
            }

            // return back the changes
            return Task.FromResult<IReadOnlyDictionary<EndPoint, EndPoint?>>(results);
        }
    }
}
