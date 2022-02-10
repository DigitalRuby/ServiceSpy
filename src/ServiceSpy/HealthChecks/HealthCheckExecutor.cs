namespace ServiceSpy.HealthChecks;

/// <summary>
/// Executes health checks against end points and flags any unhealth end points
/// </summary>
public interface IHealthCheckExecutor
{
    /// <summary>
    /// Execute a health check
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <returns>Task of string, metadata, empty string if success, otherwise an error string of why the health check failed</returns>
    Task<(ServiceMetadata, string)> Execute(ServiceMetadata metadata);
}

/// <inheritdoc />
public class HealthCheckExecutor : IHealthCheckExecutor
{
    /// <inheritdoc />
    public Task<(ServiceMetadata, string)> Execute(ServiceMetadata metadata)
    {
        return Task.FromResult<(ServiceMetadata, string)>((metadata, string.Empty));
    }
}
