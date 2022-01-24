namespace ServiceSpy.Registry;

/// <summary>
/// End point deleted event
/// </summary>
public readonly struct EndPointDeletedEvent
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The end point that was deleted
    /// </summary>
    public EndPoint EndPoint { get; init; }
}
