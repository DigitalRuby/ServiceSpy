namespace ServiceSpy.Registry;

/// <summary>
/// Respons from request to unregister a service
/// </summary>
public readonly struct UnregisterAllResponse
{
    /// <summary>
    /// The deleted end points or null if no end points were deleted
    /// </summary>
    public IReadOnlyCollection<EndPoint>? EndPoints { get; init; }
}
