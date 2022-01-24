namespace ServiceSpy.Registry;

/// <summary>
/// Manages registration information for services
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Register a service
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
    /// <param name="name">Service name</param>
    /// <returns>Service end points or null if no service found with the name</returns>
    Task<EndPoints?> GetEndpointsAsync(string name);

    /// <summary>
    /// Event for when end point is changed
    /// </summary>
    event Action<EndPointChangedEvent> EndPointChanged;

    /// <summary>
    /// Event for when end point is deleted
    /// </summary>
    event Action<EndPointDeletedEvent> EndPointDeleted;
}
