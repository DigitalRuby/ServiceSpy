﻿namespace ServiceSpy.Notifications;

/// <summary>
/// End point deleted event
/// </summary>
public readonly struct EndPointDeletedEvent
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The end point(s) that were deleted
    /// </summary>
    public IReadOnlyCollection<EndPoint> EndPoints { get; init; }

    /// <summary>
    /// Whether all the end points are deleted for the service
    /// </summary>
    public bool All { get; init; }
}
