using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Udp notification sender
/// </summary>
public sealed class UdpNotificationSender : INotificationSender, IDisposable
{
    private readonly IPEndPoint ipEndPoint;
    private readonly ILogger logger;

    private ServiceMetadata? lastMetadata;
    private string? lastHealthCheck;
    private Memory<byte> message;
    private UdpClient? server;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ipEndPoint">End point</param>
    /// <param name="logger">Logger</param>
    public UdpNotificationSender(IPEndPoint ipEndPoint, ILogger<UdpNotificationSender> logger)
    {
        this.ipEndPoint = ipEndPoint;
        this.logger = logger;
        CreateServer();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        server?.Dispose();
        server = null;
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
            await SendMessage(message, cancelToken);
        }
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
        if (message.Length == 0 || server is null)
        {
            return;
        }

        try
        {
            await server!.SendAsync(message, cancelToken);
        }
        catch (NullReferenceException)
        {
            Dispose();
        }
        catch (ObjectDisposedException)
        {
            Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast service packet, recreating server");

            // try recreating server, send message on next loop
            CreateServer();
        }
    }

    private void CreateServer()
    {
        try
        {
            server?.Dispose();
        }
        catch
        {
        }

        try
        {
            server = new UdpClient();
            server.Connect(ipEndPoint);
            server.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create service metadata broadcast server");
        }
    }
}
