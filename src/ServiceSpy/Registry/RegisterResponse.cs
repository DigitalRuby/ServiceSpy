namespace ServiceSpy.Registry;

/// <summary>
/// Respons from request to register a service
/// </summary>
public struct RegisterResponse
{
    /// <summary>
    /// Whether there was a change
    /// </summary>
    public bool Change { get; set; }

    /// <summary>
    /// The old end point or null if no previou end point
    /// </summary>
    public EndPoint? OldEndPoint { get; init; }
}
