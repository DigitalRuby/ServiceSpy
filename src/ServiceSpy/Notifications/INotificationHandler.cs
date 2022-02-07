namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to handle sending and receiving notifications about service end point changes
/// </summary>
public interface INotificationHandler
{
    /// <summary>
    /// Notify of an end point change event
    /// </summary>
    /// <param name="evt">End point changed event</param>
    void SendEndPointChanged(EndPointChangedEvent evt);

    /// <summary>
    /// Notify of an end point deletion event
    /// </summary>
    /// <param name="evt">End point deleted event</param>
    void SendEndPointDeleted(EndPointDeletedEvent evt);

    /// <summary>
    /// Receive end point changed event
    /// </summary>
    event Action<EndPointChangedEvent> ReceiveEndPointChanged;

    /// <summary>
    /// Receive end point deleted event
    /// </summary>
    event Action<EndPointDeletedEvent> ReceiveEndPointDeleted;
}
