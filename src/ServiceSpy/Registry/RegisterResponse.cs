namespace ServiceSpy.Registry;

/// <summary>
/// Respons from request to register a service
/// </summary>
public struct RegisterResponse
{
    /// <summary>
    /// Changes
    /// </summary>
    public IReadOnlyDictionary<EndPoint, EndPoint?> Changes { get; init; }
}
