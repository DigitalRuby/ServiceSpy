namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to handle sending notifications about metadata
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Send a metadata notification
    /// </summary>
    /// <param name="evt">Metadata event</param>
    /// <param name="cancelToken">Cancel token</param>
    /// <returns>Task</returns>
    Task SendMetadataAsync(MetadataNotification evt, CancellationToken cancelToken = default);
}
