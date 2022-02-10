using System.Net;
using System.Net.Sockets;

namespace ServiceSpy.Notifications.Udp;

/// <summary>
/// Receives notifications via UDP
/// </summary>
public class UdpNotificationReceiver : BackgroundService, INotificationReceiver, IDisposable
{
    private readonly int port;
    private readonly ILogger logger;

    private UdpClient? client;
    private Guid id;
    private Storage.EndPoint? lastEndPoint;

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
    public event Func<EndPointChangedEvent, CancellationToken, Task>? ReceiveEndPointChangedAsync;

    /// <inheritdoc />
    public event Func<EndPointDeletedEvent, CancellationToken, Task>? ReceiveEndPointDeletedAsync;

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
                        BinaryReader reader = new(ms, Encoding.UTF8);
                        var version = reader.Read7BitEncodedInt();

                        // validate version
                        if (version == 1)
                        {
                            var idBytesLength = reader.Read7BitEncodedInt();

                            // validate id byte length
                            if (idBytesLength == 16)
                            {
                                var idBytes = reader.ReadBytes(idBytesLength);
                                Guid id = new(idBytes);
                                bool deletion = reader.ReadBoolean();
                                var ipBytesLength = reader.Read7BitEncodedInt();

                                // valid ip address byte length
                                if (ipBytesLength == 4 || ipBytesLength == 16)
                                {
                                    var ipBytes = reader.ReadBytes(ipBytesLength);
                                    IPAddress ip = new(ipBytes);
                                    var port = reader.ReadInt32();

                                    // validate port
                                    if (port > 0 && port <= ushort.MaxValue)
                                    {
                                        var host = reader.ReadString();
                                        var path = reader.ReadString();
                                        Storage.EndPoint ep = new()
                                        {
                                            IPAddress = ip,
                                            Port = port,
                                            Host = host,
                                            Path = path
                                        };

                                        // if we changed end points, broadcast the change
                                        if (this.id != id || lastEndPoint is null || !lastEndPoint.Equals(ep))
                                        {
                                            this.id = id;
                                            if (deletion)
                                            {
                                                ReceiveEndPointChangedAsync?.Invoke(new EndPointChangedEvent
                                                {
                                                    Id = id,
                                                    Changes = new Dictionary<Storage.EndPoint, Storage.EndPoint?> { { ep, lastEndPoint } }
                                                }, stoppingToken);
                                            }
                                            else
                                            {
                                                ReceiveEndPointDeletedAsync?.Invoke(new EndPointDeletedEvent
                                                {
                                                    Id = id,
                                                    EndPoints = new Storage.EndPoint[] { ep }
                                                }, stoppingToken);
                                            }
                                            lastEndPoint = ep;
                                        }
                                    }
                                }
                            }
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
