namespace ServiceSpy.Registry;

/// <summary>
/// Request to unregister a service end points
/// </summary>
public readonly struct UnregisterAllRequest
{
    /// <summary>
    /// Service id
    /// </summary>
    public Guid Id { get; init; }
}
