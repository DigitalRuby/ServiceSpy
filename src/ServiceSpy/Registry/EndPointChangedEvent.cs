namespace ServiceSpy.Registry;

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
    /// The previous end point or null if no previous end point
    /// </summary>
    public EndPoint? OldEndPoint { get; init; }

    /// <summary>
    /// The end point that changed
    /// </summary>
    public EndPoint EndPoint { get; init; }
}
