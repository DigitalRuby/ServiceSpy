namespace ServiceSpy.Notifications;

/// <summary>
/// Service metadata notification
/// </summary>
public readonly struct MetadataNotification
{
    /// <summary>
    /// The service metadata that changed
    /// </summary>
    public ServiceMetadata Metadata { get; init; }

    /// <summary>
    /// Whether the metadata was deleted
    /// </summary>
    public bool Deleted { get; init; }

    /// <summary>
    /// Health check error, if any. Null for not specified, empty string for health otherwise an error string.
    /// </summary>
    public string? HealthCheck { get; init; }
}
