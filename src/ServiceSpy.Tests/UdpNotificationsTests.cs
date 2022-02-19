using Microsoft.Extensions.Logging.Abstractions;

using ServiceSpy.Notifications.Udp;



namespace ServiceSpy.Tests;

/// <summary>
/// Test udp send/receive notifications
/// </summary>
[TestFixture]
public sealed class UdpNotificationsTests
{
    private static readonly System.Net.IPAddress localHost = System.Net.IPAddress.Parse("127.0.0.1");
    private const int port = 51234;

    /// <summary>
    /// Test we can send/receive notifications via udp
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestSendReceive()
    {
        const int iterations = 10;
        int count = 0;
        ServiceMetadata metadata = TestUtil.CreateMetadata();
        ServiceMetadata? foundMetadata = null;
        bool deleted = false;
        string? healthCheck = null;
        using var handler = new UdpNotificationHandler(new System.Net.IPEndPoint(localHost, port), new NullLogger<UdpNotificationHandler>());
        handler.ReceiveMetadataAsync += (MetadataNotification n, CancellationToken t) =>
        {
            Interlocked.Increment(ref count);
            deleted = n.Deleted;
            healthCheck = n.HealthCheck;
            foundMetadata = n.Metadata;
            return Task.CompletedTask;
        };
        await handler.StartAsync(default);
        for (int i = 0; i < iterations; i++)
        {
            await handler.SendMetadataAsync(new MetadataNotification[]
            {
                new MetadataNotification
                {
                    Metadata = metadata
                }
            });
            await Task.Delay(20);
        }
        Console.WriteLine("Received {0}/{1} udp notifications", count, iterations);
        Assert.Greater(count, 0);
        Assert.IsFalse(deleted);
        Assert.IsNull(healthCheck);
        Assert.IsTrue(metadata.EqualsExactly(foundMetadata));
    }

    /// <summary>
    /// Test we can send and receive notifications through service loop and udp
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestServiceLoopRegistrationUdp()
    {
        ServiceMetadata metadata = TestUtil.CreateMetadata();
        UdpNotificationHandler handler = new(new System.Net.IPEndPoint(localHost, port), new NullLogger<UdpNotificationHandler>());
        HealthChecks.MetadataHealthCheckStore healthCheckStore = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100),
            new NullLogger<HealthChecks.MetadataHealthCheckStore>());
        MetadataStore store = new(handler, healthCheckStore);
        handler.ReceiveMetadataAsync += (MetadataNotification arg1, CancellationToken arg2) =>
        {
            return Task.CompletedTask;
        };
        await handler.StartAsync(default);
        ServiceRegistrationLoop loop = new(new[] { metadata }, handler, TimeSpan.FromMilliseconds(20), new NullLogger<ServiceRegistrationLoop>());
        await loop.StartAsync(default);
        await Task.Delay(100);
        var all = await store.GetMetadatasAsync();
        Assert.AreEqual(1, all.Count);
        Assert.IsTrue(all.Contains(metadata));
    }
}
