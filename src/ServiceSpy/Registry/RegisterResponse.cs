namespace ServiceSpy.Registry;

/// <summary>
/// Respons from request to register a service
/// </summary>
public struct RegisterResponse
{
    /// <summary>
    /// Changes or null if none
    /// </summary>
    public IReadOnlyDictionary<EndPoint, EndPoint?>? Changes { get; init; }
}
