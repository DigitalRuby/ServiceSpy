namespace ServiceSpy.Registry;

/// <summary>
/// Request to unregister a service end point
/// </summary>
public readonly struct UnregisterRequest
{
    /// <summary>
    /// Service name to unregister end points for
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Service end points to unregister
    /// </summary>
    public IReadOnlyCollection<EndPoint> EndPoints { get; init; }
}
