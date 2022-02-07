namespace ServiceSpy.Registry;

/// <summary>
/// Request to register a service
/// </summary>
public readonly struct RegisterRequest
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Service end points to register
    /// </summary>
    public IReadOnlyCollection<EndPoint> EndPoints { get; init; }
}
