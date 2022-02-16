namespace ServiceSpy.DependencyInjection;

/// <summary>
/// Configuration options for serivce spy
/// </summary>
public class ServiceSpyConfiguration
{
    /// <summary>
    /// Services
    /// </summary>
    public ServiceSpyServices Services { get; set; } = new();

    /// <summary>
    /// Health checks
    /// </summary>
    public ServiceSpyHealthChecks HealthChecks { get; set; } = new();

    /// <summary>
    /// Notifications
    /// </summary>
    public ServiceSpyNotifications Notifications { get; set; } = new();
}

/// <summary>
/// Service spy services configuration
/// </summary>
public class ServiceSpyServices
{
    /// <summary>
    /// Storage provider
    /// </summary>
    public string Storage { get; set; } = "InMemory";

    /// <summary>
    /// Services
    /// </summary>
    public IReadOnlyCollection<ServiceMetadata> Items { get; set; } = Array.Empty<ServiceMetadata>();
}

/// <summary>
/// Service spy health checks configuration
/// </summary>
public class ServiceSpyHealthChecks
{   
    /// <summary>
    /// Storage provider
    /// </summary>
    public string Storage { get; set; } = "InMemory";

    /// <summary>
    /// Whether to perform health checks (true) or only receive health check results (false)
    /// </summary>
    public bool PerformHealthChecks { get; set; }
}

/// <summary>
/// Service spy notifications configuration
/// </summary>
public class ServiceSpyNotifications
{
    /// <summary>
    /// Whether to both receive and broadcast notifications (true) or to only receive notifications (false).
    /// Note- health checks will always be broadcast if health check PerformHealthChecks is true.
    /// </summary>
    public bool Broadcast { get; set; }

    /// <summary>
    /// Connection info
    /// </summary>
    public ServiceSpyConnection Connection { get; set; } = new();
}

/// <summary>
/// Service spy connection details
/// </summary>
public class ServiceSpyConnection
{
    /// <summary>
    /// Protocol of connection, default is Udp
    /// </summary>
    public string Protocol { get; set; } = "Udp";

    /// <summary>
    /// IP address
    /// </summary>
    public string IPAddress { get; set; } = string.Empty;

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; set; }
}
