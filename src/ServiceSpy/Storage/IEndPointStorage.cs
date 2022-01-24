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
    /// <param name="endPoint">End point</param>
    /// <param name="oldEndPoint">Old end point or null if no old end point</param>
    /// <returns>Task of bool of whether a change was made</returns>
    Task<bool> UpsertAsync(string name, EndPoint endPoint, out EndPoint? oldEndPoint);

    /// <summary>
    /// Delete the service end point for the specified service name
    /// </summary>
    /// <param name="name">Service name</param>
    /// <param name="endPoint">End point</param>
    /// <returns>Task of bool of whether the service was found and the specified end point deleted</returns>
    Task<bool> DeleteAsync(string name, EndPoint endPoint);

    /// <summary>
    /// Delete all end points with the specified service name
    /// </summary>
    /// <param name="name">Service name</param>
    /// <returns>Task of deleted end points or null if none deleted</returns>
    Task<IReadOnlyCollection<EndPoint>?> DeleteAllAsync(string name);
}
