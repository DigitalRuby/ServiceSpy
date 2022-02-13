namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to handle sending notifications about metadata
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Send a metadata notification
    /// </summary>
    /// <param name="events">Metadata events</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task</returns>
    Task SendMetadataAsync(IEnumerable<MetadataNotification> events, CancellationToken cancelToken = default);
}
