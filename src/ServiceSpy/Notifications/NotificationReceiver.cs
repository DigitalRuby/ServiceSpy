namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to handle receiving notifications about metadata
/// </summary>
public interface INotificationReceiver
{
    /// <summary>
    /// Receive metadata event
    /// </summary>
    event Func<MetadataNotification, CancellationToken, Task> ReceiveMetadataAsync;
}
