namespace ServiceSpy.Registry;

/// <summary>
/// Request to unregister a service end point
/// </summary>
public readonly struct UnregisterRequest
{
    /// <summary>
    /// Service name to unregister an end point for
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Service end point to unregister
    /// </summary>
    public EndPoint EndPoint { get; init; }
}
