namespace ServiceSpy.Notifications;

/// <summary>
/// End point changed event
/// </summary>
public readonly struct EndPointChangedEvent
{
    /// <summary>
    /// Service id
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Contains the new end point, and previous end point (if any)
    /// </summary>
    public IReadOnlyDictionary<EndPoint, EndPoint?> Changes { get; init; }
}
