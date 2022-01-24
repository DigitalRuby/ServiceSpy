namespace ServiceSpy.Registry;

/// <summary>
/// Request to unregister a service end points
/// </summary>
public readonly struct UnregisterAllRequest
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; init; }
}
