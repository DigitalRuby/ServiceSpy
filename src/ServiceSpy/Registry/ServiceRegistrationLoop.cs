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
    private readonly ServiceMetadata metadata;
    private readonly INotificationSender handler;
    private readonly TimeSpan interval;
    private readonly ILogger logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="metadata">Service metadata</param>
    /// <param name="handler">Sends change notifications</param>
    /// <param name="interval">Send interval</param>
    /// <param name="logger">Logger</param>
    public ServiceRegistrationLoop(ServiceMetadata metadata,
        INotificationSender handler,
        TimeSpan interval,
        ILogger<ServiceRegistrationLoop> logger)
    {
        this.metadata = metadata;
        this.handler = handler;
        this.interval = interval;
        this.logger = logger;
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // send shutdown/deletion event
        await handler.SendMetadataAsync(new MetadataNotification[]
        {
            new MetadataNotification
            {
                Deleted = true,
                Metadata = metadata
            }
        }, cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // send out our service metadata to the universe
                await handler.SendMetadataAsync(new MetadataNotification[]
                {
                    new MetadataNotification
                    {
                        Metadata = metadata
                    }
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending metadata event");
            }

            await Task.Delay(interval, stoppingToken);
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
