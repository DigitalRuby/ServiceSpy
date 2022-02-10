namespace ServiceSpy.Storage;

/// <summary>
/// Stores end point info in memory
/// </summary>
public sealed class InMemoryEndPointStorage : IEndPointStorage
{
    private readonly Dictionary<Guid, EndPoints> allEndPoints = new();

    /// <inheritdoc />
    public Task<IReadOnlyCollection<EndPoint>?> DeleteAllAsync(Guid id, CancellationToken cancelToken)
    {
        lock (allEndPoints)
        {
            allEndPoints.Remove(id, out EndPoints? endPoints);
            return Task.FromResult<IReadOnlyCollection<EndPoint>?>(endPoints?.GetAllEndPoints());
        }
    }

    /// <inheritdoc />
    public Task<(bool, bool)> DeleteAsync(Guid id, IEnumerable<EndPoint> endPoints, CancellationToken cancelToken)
    {
        lock (allEndPoints)
        {
            bool removed = false;
            bool empty = false;
            if (this.allEndPoints.TryGetValue(id, out EndPoints? allEndPoints))
            {
                foreach (var endPoint in endPoints)
                {
                    removed |= allEndPoints.Remove(endPoint, out bool _empty);
                    empty |= _empty;
                }
                if (empty)
                {
                    this.allEndPoints.Remove(id);
                }
            }
            return Task.FromResult<(bool, bool)>((removed, empty));
        }
    }

    /// <inheritdoc />
    public Task<EndPoints?> GetAsync(Guid id, CancellationToken cancelToken)
    {
        lock (allEndPoints)
        {
            if (allEndPoints.TryGetValue(id, out EndPoints? endPoints))
            {
                return Task.FromResult<EndPoints?>(endPoints);
            }
        }
        return Task.FromResult<EndPoints?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<EndPoint, EndPoint?>?> UpsertAsync(Guid id, IEnumerable<EndPoint> endPoints, CancellationToken cancelToken)
    {
        lock (allEndPoints)
        {
            // make end points if we need to
            if (!allEndPoints.TryGetValue(id, out EndPoints? currentEndPoints))
            {
                allEndPoints[id] = currentEndPoints = new(id);
            }

            // results
            Dictionary<EndPoint, EndPoint?>? results = null;

            // upsert each end point
            foreach (var endPoint in endPoints)
            {
                bool change = currentEndPoints.Upsert(endPoint, out EndPoint? oldEndPoint);
                if (change)
                {
                    results ??= new Dictionary<EndPoint, EndPoint?>();
                    results[endPoint] = oldEndPoint;
                }
            }

            // return back the changes
            return Task.FromResult<IReadOnlyDictionary<EndPoint, EndPoint?>?>(results);
        }
    }
}
