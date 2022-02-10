using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Udp notification sender
/// </summary>
public class UdpNotificationSender : INotificationSender, IDisposable
{
    private readonly int port;
    private readonly ILogger logger;

    private Guid id;
    private Storage.EndPoint lastEndPoint;
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
    public Task SendEndPointChangedAsync(EndPointChangedEvent evt, CancellationToken cancelToken)
    {
        if (evt.Changes is null || evt.Changes.Count != 1)
        {
            return Task.CompletedTask;
        }

        // see if message changed, if so make a new one, else send existing message
        var ep = evt.Changes.First().Key;
        if (id != evt.Id || !lastEndPoint.Equals(ep))
        {
            lastEndPoint = ep;
            id = evt.Id;
            message = CreateMessage(false);
        }

        return SendMessage(message, cancelToken);
    }

    /// <inheritdoc />
    public async Task SendEndPointDeletedAsync(EndPointDeletedEvent evt, CancellationToken cancelToken)
    {
        if (evt.EndPoints is not null && evt.EndPoints.Count != 0)
        {
            var ep = evt.EndPoints.First();
            if (lastEndPoint.Equals(ep))
            {
                // we are done
                var message = CreateMessage(true);
                await SendMessage(message, cancelToken);
                Dispose();
            }
        }
    }

    private Memory<byte> CreateMessage(bool deletion)
    {
        var guidBytes = id.ToByteArray();
        MemoryStream ms = new();
        BinaryWriter writer = new(ms, Encoding.UTF8);
        writer.Write7BitEncodedInt(1); // version
        writer.Write7BitEncodedInt(guidBytes.Length); // id length
        writer.Write(guidBytes); // id
        writer.Write(deletion);
        var ipBytes = lastEndPoint.IPAddress.GetAddressBytes();
        writer.Write7BitEncodedInt(ipBytes.Length); // ip address byte length
        writer.Write(ipBytes); // ip address bytes
        writer.Write(port);
        writer.Write(lastEndPoint.Host); // host length + host bytes
        writer.Write(lastEndPoint.Path); // path length + path bytes
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
