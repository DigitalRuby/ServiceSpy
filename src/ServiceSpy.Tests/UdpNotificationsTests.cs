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

    [Test]
    public async Task TestSendReceive()
    {
        const int iterations = 10;
        int count = 0;
        ServiceMetadata metadata = TestUtil.CreateMetadata();
        ServiceMetadata? foundMetadata = null;
        bool deleted = false;
        string? healthCheck = null;
        using var receiver = new UdpNotificationReceiver(new System.Net.IPEndPoint(localHost, port), new NullLogger<UdpNotificationReceiver>());
        receiver.ReceiveMetadataAsync += (MetadataNotification n, CancellationToken t) =>
        {
            Interlocked.Increment(ref count);
            deleted = n.Deleted;
            healthCheck = n.HealthCheck;
            foundMetadata = n.Metadata;
            return Task.CompletedTask;
        };
        await receiver.StartAsync(default);
        using var sender = new UdpNotificationSender(new System.Net.IPEndPoint(localHost, port), new NullLogger<UdpNotificationSender>());
        for (int i = 0; i < iterations; i++)
        {
            await sender.SendMetadataAsync(new MetadataNotification { Metadata = metadata }, default);
            await Task.Delay(20);
        }
        Console.WriteLine("Received {0}/{1} udp notifications", count, iterations);
        Assert.Greater(count, 0);
        Assert.IsFalse(deleted);
        Assert.IsNull(healthCheck);
        Assert.IsTrue(metadata.EqualsExactly(foundMetadata));
    }
}
