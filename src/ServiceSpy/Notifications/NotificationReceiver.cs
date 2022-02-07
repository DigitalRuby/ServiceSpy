namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to handle receiving notifications about service end point changes
/// </summary>
public interface INotificationReceiver
{
    /// <summary>
    /// Receive end point changed event
    /// </summary>
    event Func<EndPointChangedEvent, Task> ReceiveEndPointChangedAsync;

    /// <summary>
    /// Receive end point deleted event
    /// </summary>
    event Func<EndPointDeletedEvent, Task> ReceiveEndPointDeletedAsync;
}
