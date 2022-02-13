namespace ServiceSpy.HealthChecks;

/// <summary>
/// Health check status
/// </summary>
public sealed class HealthCheckStatus
{
    internal void Clear()
    {
        LastHealthCheck = DateTimeOffset.UtcNow;
        LastError = string.Empty;
        Failures = 0;
    }

    internal void Fail(string error)
    {
        Failures++;
        LastError = error;
        LastHealthCheck = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTimeOffset LastHealthCheck { get; internal set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last error or empty string if no error
    /// </summary>
    public string LastError { get; internal set; } = string.Empty;

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int Failures { get; internal set; }
}