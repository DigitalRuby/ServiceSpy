using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Udp notification sender
/// </summary>
public sealed class UdpNotificationSender : INotificationSender, IDisposable
{
    internal static readonly byte[] serviceSpyGuid = Guid.Parse("62573135-7B6A-4FAC-B765-9BE43E83E444").ToByteArray();

    private readonly int port;
    private readonly ILogger logger;

    private ServiceMetadata? lastMetadata;
    private Memory<byte> message;
    private UdpClient? server;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="port">Port to broadcast on</param>
    /// <param name="logger">Logger</param>
    public UdpNotificationSender(int port, ILogger<UdpNotificationSender> logger)
    {
        this.port = port;
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
    public Task SendMetadataAsync(MetadataNotification evt, CancellationToken cancelToken)
    {
        if (lastMetadata is null || !lastMetadata.Equals(evt.Metadata))
        {
            lastMetadata = evt.Metadata;
            message = CreateMessage(evt.Deleted);
        }
        return SendMessage(message, cancelToken);
    }

    private Memory<byte> CreateMessage(bool deletion)
    {
        var guidBytes = lastMetadata!.Id.ToByteArray();
        MemoryStream ms = new();
        BinaryWriter writer = new(ms, Encoding.UTF8);
        writer.Write7BitEncodedInt(serviceSpyGuid.Length);
        writer.Write(serviceSpyGuid);
        writer.Write7BitEncodedInt(1); // version
        writer.Write7BitEncodedInt(guidBytes.Length); // id length
        writer.Write(guidBytes); // id
        writer.Write(lastMetadata.Name); // name
        writer.Write(lastMetadata.Version); // service version
        writer.Write(deletion); // is this a deletion?
        var ipBytes = lastMetadata.IPAddress.GetAddressBytes();
        writer.Write7BitEncodedInt(ipBytes.Length); // ip address byte length
        writer.Write(ipBytes); // ip address bytes
        writer.Write(port); // port
        writer.Write(lastMetadata.Host); // host length + host bytes
        writer.Write(lastMetadata.Path); // path length + path bytes
        writer.Write(lastMetadata.HealthCheckPath); // health check path length + health check path bytes
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
            IPEndPoint endPoint = new(System.Net.IPAddress.Broadcast, port);
            server = new UdpClient();
            server.Connect(endPoint);
            server.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create service metadata broadcast server");
        }
    }
}
