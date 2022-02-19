using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Receives notifications via UDP
/// </summary>
public sealed class UdpNotificationHandler : BackgroundService, INotificationReceiver, INotificationSender, IDisposable
{
    private readonly IPEndPoint ipEndPointBind;
    //private readonly IPEndPoint ipEndPointReceive;
    private readonly IPEndPoint ipEndPointSend;
    private readonly ILogger logger;

    private UdpClient? udpClient;
    private ServiceMetadata? lastMetadata;
    private string? lastHealthCheck;
    private Memory<byte> message;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ipEndPoint">End point to bind to</param>
    /// <param name="logger">Logger</param>
    public UdpNotificationHandler(IPEndPoint ipEndPoint, ILogger<UdpNotificationHandler> logger)
    {
        this.ipEndPointBind = ipEndPoint;
        //this.ipEndPointReceive = new(0, 0);
        this.ipEndPointSend = new(IPAddress.Broadcast, ipEndPoint.Port);
        this.logger = logger;
        CreateUdpClient();
    }

    /// <inheritdoc />
    public async Task SendMetadataAsync(IEnumerable<MetadataNotification> events, CancellationToken cancelToken = default)
    {
        foreach (var notification in events)
        {
            if (lastMetadata is null ||
                notification.Deleted ||
                notification.HealthCheck != lastHealthCheck ||
                !lastMetadata.Equals(notification.Metadata))
            {
                lastMetadata = notification.Metadata;
                lastHealthCheck = notification.HealthCheck;
                message = CreateMessage(notification.Deleted, notification.HealthCheck);
            }

            logger.LogDebug("Sending metadata: " + lastMetadata + ", deleted: " + notification.Deleted +
                ", health-check: " + (notification.HealthCheck == null ? "N/A" : (notification.HealthCheck == string.Empty ? "OK" : notification.HealthCheck)));
            await SendMessage(message, cancelToken);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        lock (this)
        {
            if (udpClient is not null)
            {
                GC.SuppressFinalize(this);
                udpClient?.Dispose();
                udpClient = null;
                base.Dispose();
            }
        }
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
                var result = await udpClient!.ReceiveAsync(stoppingToken);
                byte[] bytes = result.Buffer;
                try
                {
                    // max packet length 1024
                    if (bytes.Length < 1024)
                    {
                        MemoryStream ms = new(bytes);
                        var newMetadata = ServiceMetadata.FromBinary(ms, out bool deletion, out string? healthCheck);
                        if (newMetadata is not null)
                        {
                            logger.LogDebug("Received metadata: " + newMetadata + ", deleted: " + deletion +
                                ", health-check: " + (healthCheck == null ? "N/A" : (healthCheck == string.Empty ? "OK" : healthCheck)));
                            ReceiveMetadataAsync?.Invoke(new MetadataNotification
                            {
                                Metadata = newMetadata,
                                Deleted = deletion,
                                HealthCheck = healthCheck
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
            catch (OperationCanceledException)
            {
                // we are done
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving service metadata packet");

                // give a few seconds before trying again
                await Task.Delay(5000, stoppingToken);

                CreateUdpClient();
            }
            await Task.Delay(20, stoppingToken);
        }

        Dispose();
    }

    private Memory<byte> CreateMessage(bool deletion, string? healthCheck)
    {
        if (lastMetadata is null)
        {
            return Memory<byte>.Empty;
        }

        MemoryStream ms = new();
        lastMetadata.ToBinary(ms, deletion, healthCheck);
        return ms.ToArray();
    }

    private async Task SendMessage(Memory<byte> message, CancellationToken cancelToken)
    {
        if (message.Length == 0 || udpClient is null)
        {
            return;
        }

        try
        {
            await udpClient!.SendAsync(message, ipEndPointSend, cancelToken);
        }
        catch (NullReferenceException)
        {
            Dispose();
        }
        catch (ObjectDisposedException)
        {
            Dispose();
        }
        catch (OperationCanceledException)
        {
            Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast service packet, recreating server");

            // try recreating server, send message on next loop
            CreateUdpClient();
        }
    }

    private void CreateUdpClient()
    {
        lock (this)
        {
            try
            {
                udpClient?.Dispose();
            }
            catch
            {
            }

            try
            {
                udpClient = new UdpClient()
                {
                    EnableBroadcast = true,
                    MulticastLoopback = true
                };
                udpClient.Client.Bind(ipEndPointBind);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create service metadata receiver client");
            }
        }
    }
}
