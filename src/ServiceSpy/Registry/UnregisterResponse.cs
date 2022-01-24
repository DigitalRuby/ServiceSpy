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
}
