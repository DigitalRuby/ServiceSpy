namespace ServiceSpy.Registry;

/// <summary>
/// Respons from request to unregister a service
/// </summary>
public readonly struct UnregisterResponse
{
    /// <summary>
    /// Whether the unregister resulted in a deletion
    /// </summary>
    public bool Deleted { get; init; }

    /// <summary>
    /// Whether all end points are now unregistered
    /// </summary>
    public bool All { get; init; }
}
