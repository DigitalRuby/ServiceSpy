namespace ServiceSpy.Notifications;

/// <summary>
/// End point changed event
/// </summary>
public readonly struct EndPointChangedEvent
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Contains the new end point, and previous end point (if any)
    /// </summary>
    public IReadOnlyDictionary<EndPoint, EndPoint?> Changes { get; init; }
}
