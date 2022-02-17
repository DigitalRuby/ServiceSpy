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
    /// Interval to perform health checks (seconds), or less than equal 0 to not perform health checks
    /// </summary>
    public int HealthCheckInterval { get; set; }

    /// <summary>
    /// Amount of time (seconds) to cache healthy health checks
    /// </summary>
    public int HealthyCacheTime { get; set; } = 5;

    /// <summary>
    /// Cleanup interval (seconds) to check for expired health checks
    /// </summary>
    public int CleanupInterval { get; set; } = 5;

    /// <summary>
    /// Number of seconds after which a metadata with no health check result will be purged
    /// </summary>
    public int ExpireTime { get; set; } = 300;
}

/// <summary>
/// Service spy notifications configuration
/// </summary>
public class ServiceSpyNotifications
{
    /// <summary>
    /// The interval (in seconds) to broadcast notifications of service metadata, or less than equal 0 to receive only.
    /// Note- health checks will always be broadcast if health check PerformHealthChecks is true.
    /// </summary>
    public int BroadcastInterval { get; set; }

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
    /// Get parsed ip address from IPAddress property
    /// </summary>
    public System.Net.IPAddress ParsedIPAddress =>
        (string.IsNullOrWhiteSpace(IPAddress) || IPAddress == "*" ? ServiceMetadata.GetLocalIPAddress() : System.Net.IPAddress.Parse(IPAddress));

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; set; }
}
