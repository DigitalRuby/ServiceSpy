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
}
