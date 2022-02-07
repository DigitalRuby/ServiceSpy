namespace ServiceSpy.Storage;

/// <summary>
/// Contains endpoints for a service
/// </summary>
public sealed class EndPoints
{
    private readonly Dictionary<System.Net.IPAddress, EndPoint> endPoints = new();

    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Get all the end points available for the service
    /// </summary>
    public IReadOnlyCollection<EndPoint> GetAllEndPoints()
    {
        lock (endPoints)
        {
            return endPoints.Values.ToArray();
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Service name</param>
    public EndPoints(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Add (or update) the end point
    /// </summary>
    /// <param name="endPoint">End point</param>
    /// <param name="oldEndPoint">Old end point or null if no old end point</param>
    /// <returns>True if a change was performed, false otherwise</returns>
    public bool Upsert(EndPoint endPoint, out EndPoint? oldEndPoint)
    {
        lock (endPoints)
        {
            bool removed = endPoints.TryGetValue(endPoint.IPAddress, out EndPoint foundEndPoint);
            bool change = !removed ||
                foundEndPoint.Port != endPoint.Port ||
                !foundEndPoint.Host.Equals(endPoint.Host, StringComparison.OrdinalIgnoreCase) ||
                !foundEndPoint.Path.Equals(endPoint.Path, StringComparison.OrdinalIgnoreCase);
            endPoints[endPoint.IPAddress] = endPoint;
            oldEndPoint = (removed ? foundEndPoint : null);
            return change;
        }
    }

    /// <summary>
    /// Remove the end point
    /// </summary>
    /// <param name="endPoint">End point to remove</param>
    /// <param name="empty">True if empty after remove, false if not</param>
    /// <returns>True if the end point was found and removed, false otherwise</returns>
    public bool Remove(EndPoint endPoint, out bool empty)
    {
        lock (endPoints)
        {
            bool removed = endPoints.Remove(endPoint.IPAddress);
            empty = endPoints.Count == 0;
            return removed;
        }
    }
}

