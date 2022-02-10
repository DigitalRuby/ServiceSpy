namespace ServiceSpy.Registry;

/// <summary>
/// Registers an instance of a service on a timer
/// </summary>
public interface IServiceRegistrationLoop : IDisposable
{
}


/// <inheritdoc />
public class ServiceRegistrationLoop : BackgroundService, IServiceRegistrationLoop
{
    private readonly ServiceRegistrationHandlerConfig config;
    private readonly INotificationSender handler;
    private readonly ILogger logger;
    private readonly Dictionary<EndPoint, EndPoint?> changes;
    private readonly EndPoint endPoint;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config">Config</param>
    /// <param name="handler">Sends change notifications</param>
    public ServiceRegistrationLoop(ServiceRegistrationHandlerConfig config, INotificationSender handler, ILogger<ServiceRegistrationLoop> logger)
    {
        this.config = config;
        this.handler = handler;
        this.logger = logger;
        string ipAddress = (string.IsNullOrWhiteSpace(config.IPAddress) ? GetLocalIPAddress() : config.IPAddress);
        endPoint = new()
        {
            Host = config.Host,
            IPAddress = System.Net.IPAddress.Parse(ipAddress),
            Path = config.Path,
            Port = config.Port
        };
        changes = new();
        changes[endPoint] = null;
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // send shutdown/deletion event
        await handler.SendEndPointDeletedAsync(new EndPointDeletedEvent
        {
            Id = config.Id,
            EndPoints = new EndPoint[] { changes.First().Key }
        }, cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        changes[endPoint] = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // send out our service config and end point to the universe
                await handler.SendEndPointChangedAsync(new EndPointChangedEvent
                {
                    Id = config.Id,
                    Changes = changes
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending end point changed event");
            }

            await Task.Delay(config.Interval, stoppingToken);
        }
    }

    /// <summary>
    /// Get local machine ip
    /// </summary>
    /// <returns>Local machine ip</returns>
    /// <exception cref="ApplicationException">Failed to find local ip</exception>
    public static string GetLocalIPAddress()
    {
        // try ipv4
        using (System.Net.Sockets.Socket socket = new (System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
            if (endPoint is not null)
            {
                return endPoint.Address.ToString();
            }
        }

        // try ipv6
        using (System.Net.Sockets.Socket socket = new (System.Net.Sockets.AddressFamily.InterNetworkV6, System.Net.Sockets.SocketType.Dgram, 0))
        {
            socket.Connect("2001:4860:4860::8888", 65530);
            var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
            if (endPoint is not null)
            {
                return endPoint.Address.ToString();
            }
        }

        // ruh roh
        throw new ApplicationException("No network adapters with an IPv4 or IPv6 address on the system");
    }
}

/// <inheritdoc />
public class ServiceRegistrationHandlerConfig
{
    private static readonly TimeSpan defaultInterval = TimeSpan.FromSeconds(10.0);

    /// <summary>
    /// Service id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Host name (for http host header)
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Base path for http calls
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// IP address or empty to pick one
    /// </summary>
    public string IPAddress { get; set; } = string.Empty;

    /// <summary>
    /// Port (default is 443 https)
    /// </summary>
    public int Port { get; set; } = 443;

    /// <summary>
    /// Interval to send registration notification. Default is 10 seconds.
    /// </summary>
    public TimeSpan Interval { get; set; } = defaultInterval;
}
