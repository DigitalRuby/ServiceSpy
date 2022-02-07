namespace ServiceSpy.Registry;

/// <summary>
/// Manages registration information for services
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Register a service end point
    /// </summary>
    /// <param name="request">Register request</param>
    /// <returns>Register response</returns>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Unregister a service end point
    /// </summary>
    /// <param name="request">Unregister request</param>
    /// <returns>Unregister response</returns>
    Task<UnregisterResponse> UnregisterAsync(UnregisterRequest request);

    /// <summary>
    /// Unregister all service end points
    /// </summary>
    /// <param name="request">Unregister all request</param>
    /// <returns>Unregister all response</returns>
    Task<UnregisterAllResponse> UnregisterAllAsync(UnregisterAllRequest request);

    /// <summary>
    /// Find endpoints for a service
    /// </summary>
    /// <param name="id">Service id</param>
    /// <returns>Service end points or null if no service found with the name</returns>
    Task<EndPoints?> GetEndpointsAsync(Guid id);
}
