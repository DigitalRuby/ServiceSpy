namespace ServiceSpy.Tests;

/// <summary>
/// Test utilities
/// </summary>
public static class TestUtil
{
    internal static readonly Guid id = Guid.NewGuid();
    internal static readonly string host = "www.digitalruby.com";
    internal static readonly System.Net.IPAddress ipAddress = System.Net.IPAddress.Parse("55.55.55.55");
    internal static readonly string name = "name";
    internal static readonly string path = "/path";
    internal static readonly string healthCheckPath = "/health-check";
    internal static readonly int port = 80;
    internal static readonly string version = "42.69.1";

    /// <summary>
    /// Create a test service metadata
    /// </summary>
    /// <param name="id">Override id</param>
    /// <param name="ip">Override ip</param>
    /// <param name="port"">Override port</param>
    /// <param name="name">Override name</param>
    /// <param name="host">Override host</param>
    /// <param name="path">Override path</param>
    /// <param name="healthCheckPath">Override health check path</param>
    /// <param name="version">Override version</param>
    /// <returns>Test service metadata</returns>
    public static ServiceMetadata CreateMetadata(Guid? id = null,
        string? ip = null,
        int? port = null,
        string? name = null,
        string? host = null,
        string? path = null,
        string? healthCheckPath = null,
        string? version = null)
    {
        return new ServiceMetadata
        {
            Id = id ?? TestUtil.id,
            Host = host ?? TestUtil.host,
            IPAddress = string.IsNullOrWhiteSpace(ip) ? ipAddress : System.Net.IPAddress.Parse(ip),
            Name = name ?? TestUtil.name,
            Path = path ?? TestUtil.path,
            HealthCheckPath = healthCheckPath ?? TestUtil.healthCheckPath,
            Port = port ?? TestUtil.port,
            Version = version ?? TestUtil.version
        };
    }
}
