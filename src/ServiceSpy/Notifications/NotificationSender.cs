﻿namespace ServiceSpy.Notifications;

/// <summary>
/// Interface to send notifications about service end point changes
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Notify of an end point change event
    /// </summary>
    /// <param name="evt">End point changed event</param>
    /// <returns>Task</returns>
    Task SendEndPointChangedAsync(EndPointChangedEvent evt);

    /// <summary>
    /// Notify of an end point deletion event
    /// </summary>
    /// <param name="evt">End point deleted event</param>
    /// <returns>Task</returns>
    Task SendEndPointDeletedAsync(EndPointDeletedEvent evt);
}