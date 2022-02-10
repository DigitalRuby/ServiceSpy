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
                        BinaryReader reader = new(ms, Encoding.UTF8);
                        var serviceSpyGuidLength = reader.Read7BitEncodedInt();
                        if (serviceSpyGuidLength == 16)
                        {
                            var serviceSpyGuidBytes = reader.ReadBytes(serviceSpyGuidLength);
                            if (serviceSpyGuidBytes.SequenceEqual(UdpNotificationSender.serviceSpyGuid))
                            {
                                var version = reader.Read7BitEncodedInt();

                                // validate version
                                if (version == 1)
                                {
                                    // service id length
                                    var idBytesLength = reader.Read7BitEncodedInt();

                                    // validate id byte length
                                    if (idBytesLength == 16)
                                    {
                                        // service id
                                        var idBytes = reader.ReadBytes(idBytesLength);
                                        Guid id = new(idBytes);

                                        var name = reader.ReadString(); // name
                                        var serviceVersion = reader.ReadString(); // service version
                                        var deletion = reader.ReadBoolean(); // is this a deletion?
                                        var ipBytesLength = reader.Read7BitEncodedInt(); // ip address byte length

                                        // valid ip address byte length
                                        if (ipBytesLength == 4 || ipBytesLength == 16)
                                        {
                                            // ip bytes
                                            var ipBytes = reader.ReadBytes(ipBytesLength);
                                            IPAddress ip = new(ipBytes);

                                            // port
                                            var port = reader.ReadInt32();

                                            // validate port
                                            if (port > 0 && port <= ushort.MaxValue)
                                            {
                                                // host
                                                var host = reader.ReadString();

                                                // validate host
                                                if (host.Length < 128)
                                                {
                                                    // root path
                                                    var path = reader.ReadString();

                                                    /// validate path
                                                    if (path.Length < 128)
                                                    {
                                                        // root health check path
                                                        var healthCheckPath = reader.ReadString();

                                                        // validate health check path
                                                        if (healthCheckPath.Length < 128)
                                                        {
                                                            ServiceMetadata newMetadata = new()
                                                            {
                                                                Id = id,
                                                                Name = name,
                                                                Version = serviceVersion,
                                                                IPAddress = ip,
                                                                Port = port,
                                                                Host = host,
                                                                Path = path,
                                                                HealthCheckPath = healthCheckPath
                                                            };

                                                            // if we changed end points, broadcast the change
                                                            if (lastMetadata is null || !lastMetadata.Equals(newMetadata))
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
                                                }
                                            }
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
