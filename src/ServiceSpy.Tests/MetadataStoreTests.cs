using System.Collections.Generic;

namespace ServiceSpy.Tests;

/// <summary>
/// Metadata store tests
/// </summary>
[TestFixture]
public sealed class MetadataStoreTests : INotificationSender, INotificationReceiver, HealthChecks.IMetadataHealthCheckStore
{
    private MetadataStore? metadataStore;

    /// <summary>
    /// SetUp
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        metadataStore = new(this, this);
    }

    /// <summary>
    /// Test metadata store
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestMetadataStore()
    {
        var metadata1 = TestUtil.CreateMetadata();
        var metadata2 = TestUtil.CreateMetadata(name: "Test123"); // same as metadata1 with new name
        var metadata3 = TestUtil.CreateMetadata(id: Guid.NewGuid(), ip: "22.23.24.25", port: 443, name: "Test12345");
        var metadata4 = TestUtil.CreateMetadata(id: Guid.NewGuid(), ip: "12.23.24.25", port: 42, name: "Test123456");

        // no metadatas
        Assert.IsFalse(await metadataStore!.RemoveAsync(metadata1));

        // add a metadata
        await metadataStore.UpsertAsync(metadata1);

        // should not remove not exists metadata
        Assert.IsFalse(await metadataStore!.RemoveAsync(metadata4));

        // should remove exists metadata
        Assert.IsTrue(await metadataStore!.RemoveAsync(metadata1));
        await metadataStore.UpsertAsync(metadata1);

        // add same metadata with new name
        await metadataStore.UpsertAsync(metadata2);
        var all = await metadataStore.GetMetadatasAsync();
        Assert.AreEqual(1, all.Count);

        // add a different metadata
        await metadataStore.UpsertAsync(metadata3);
        all = await metadataStore.GetMetadatasAsync();
        Assert.AreEqual(2, all.Count);
        Assert.IsTrue(all.Contains(metadata1));
        Assert.IsTrue(all.Contains(metadata2));
        Assert.IsTrue(all.Contains(metadata3));

        // add metadata through notification
        ReceiveMetadataAsync?.Invoke(new MetadataNotification
        {
            Metadata = metadata4
        }, default);
        all = await metadataStore.GetMetadatasAsync();
        Assert.AreEqual(3, all.Count);
        Assert.IsTrue(all.Contains(metadata1));
        Assert.IsTrue(all.Contains(metadata2));
        Assert.IsTrue(all.Contains(metadata3));
        Assert.IsTrue(all.Contains(metadata4));
    }

    /// <summary>
    /// Test service registration loop
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestServiceRegistrationLoop()
    {
        var metadata1 = TestUtil.CreateMetadata();
        using ServiceRegistrationLoop loop = new(metadata1, this, TimeSpan.FromMilliseconds(20), new NullLogger<ServiceRegistrationLoop>());
        await loop.StartAsync(default);
        await Task.Delay(100);
        var all = await metadataStore!.GetMetadatasAsync();
        Assert.AreEqual(1, all.Count);
        Assert.IsTrue(all.Contains(metadata1));
    }


    /// <inheritdoc />
    public event Func<MetadataNotification, CancellationToken, Task>? ReceiveMetadataAsync;

    /// <inheritdoc />
    public Task SetHealthAsync(IEnumerable<(ServiceMetadata metadata, string error)> results, CancellationToken cancelToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendMetadataAsync(IEnumerable<MetadataNotification> events, CancellationToken cancelToken = default)
    {
        foreach (var evt in events)
        {
            await metadataStore!.UpsertAsync(evt.Metadata, cancelToken);
        }
    }

    /// <inheritdoc />
    public Task<string?> GetHealthAsync(ServiceMetadata metadata, CancellationToken cancelToken = default)
    {
        return Task.FromResult<string?>(null);
    }
}
