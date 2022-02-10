using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Receives notifications via UDP
/// </summary>
public sealed class UdpNotificationReceiver : BackgroundService, INotificationReceiver, IDisposable
{
    private readonly int port;
    private readonly ILogger logger;

    private UdpClient? client;
    private ServiceMetadata? lastMetadata;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="port">Port</param>
    /// <param name="logger">Logger</param>
    public UdpNotificationReceiver(int port, ILogger<UdpNotificationReceiver> logger)
    {
        this.port = port;
        this.logger = logger;
        CreateClient();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        client?.Dispose();
        client = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public event Func<MetadataNotification, CancellationToken, Task>? ReceiveMetadataAsync;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await client!.ReceiveAsync(stoppingToken);
                byte[] bytes = result.Buffer;
                try
                {
                    // max packet length 1024
                    if (bytes.Length < 1024)
                    {
                        MemoryStream ms = new(bytes);
                        var newMetadata = ServiceMetadata.FromBinary(ms, out bool deletion);

                        // if we changed end points, broadcast the change
                        if (newMetadata is not null &&
                            (lastMetadata is null || !lastMetadata.Equals(newMetadata)))
                        {
                            lastMetadata = newMetadata;
                            ReceiveMetadataAsync?.Invoke(new MetadataNotification
                            {
                                Metadata = newMetadata,
                                Deleted = deletion
                            }, stoppingToken);
                        }
                    }
                }
                catch
                {
                    // failed to create message, udp is not reliable and there may be other types of messages so no biggie
                }
            }
            catch (NullReferenceException)
            {
                // we are done
                break;
            }
            catch (ObjectDisposedException)
            {
                // we are done
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving service metadata packet");

                // give a few seconds before trying again
                await Task.Delay(5000, stoppingToken);

                CreateClient();
            }
            await Task.Delay(20, stoppingToken);
        }
    }

    private void CreateClient()
    {
        try
        {
            client?.Dispose();
        }
        catch
        {
        }

        try
        {
            IPEndPoint endPoint = new(IPAddress.Any, port);
            client = new UdpClient();
            client.Client.Bind(endPoint);
            client.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create service metadata receiver client");
        }
    }
}
