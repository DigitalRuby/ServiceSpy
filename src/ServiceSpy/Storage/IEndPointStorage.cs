namespace ServiceSpy.Storage;

/// <summary>
/// Storage for service end points
/// </summary>
public interface IEndPointStorage
{
    /// <summary>
    /// Get all the end points for a service
    /// </summary>
    /// <param name="name">Service name</param>
    /// <returns>Endpoints or null if none found</returns>
    Task<EndPoints?> GetAsync(string name);

    /// <summary>
    /// Upsert the specified service name and end point. If the end point already exists for the given service name, nothing happens.
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="endPoints">End points to upsert</param>
    /// <returns>Task of dictionary of changes (new, old end point) or null if no changes. End points that are not modified will not be included in this result.</returns>
    Task<IReadOnlyDictionary<EndPoint, EndPoint?>?> UpsertAsync(string name, IEnumerable<EndPoint> endPoints);

    /// <summary>
    /// Delete the service end point for the specified service name
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="endPoint">End points to delete</param>
    /// <returns>Task of bool of whether the service was found and the specified end point deleted and another bool indicating whether all end points are deleted</returns>
    Task<(bool, bool)> DeleteAsync(string name, IEnumerable<EndPoint> endPoints);

    /// <summary>
    /// Delete all end points with the specified service name
    /// </summary>
    /// <param name="name">Service name</param>
    /// <returns>Task of deleted end points or null if none deleted</returns>
    Task<IReadOnlyCollection<EndPoint>?> DeleteAllAsync(string name);
}
