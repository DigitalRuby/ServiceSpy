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
    private readonly ServiceMetadata[] metadatas;
    private readonly INotificationSender handler;
    private readonly TimeSpan interval;
    private readonly ILogger logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="metadatas">Service metadatas</param>
    /// <param name="handler">Sends change notifications</param>
    /// <param name="interval">Send interval</param>
    /// <param name="logger">Logger</param>
    public ServiceRegistrationLoop(IEnumerable<ServiceMetadata> metadatas,
        INotificationSender handler,
        TimeSpan interval,
        ILogger<ServiceRegistrationLoop> logger)
    {
        this.metadatas = metadatas.ToArray();
        this.handler = handler;
        this.interval = interval;
        this.logger = logger;
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // send shutdown/deletion events
        MetadataNotification[] metadataNotifications = new MetadataNotification[metadatas.Length];
        for (int i = 0; i < metadatas.Length; i++)
        {
            metadataNotifications[i] = new MetadataNotification
            {
                Deleted = true,
                Metadata = metadatas[i]
            };
        };
        await handler.SendMetadataAsync(metadataNotifications, cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // create notifications up front once
        MetadataNotification[] metadataNotifications = new MetadataNotification[metadatas.Length];
        for (int i = 0; i < metadatas.Length; i++)
        {
            metadataNotifications[i] = new MetadataNotification
            {
                Metadata = metadatas[i]
            };
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // send out our service metadata to the universe
                await handler.SendMetadataAsync(metadataNotifications, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending metadata event");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
