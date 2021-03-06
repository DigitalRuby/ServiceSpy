using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using Moq;

using ServiceSpy.HealthChecks;

namespace ServiceSpy.Tests;

/// <summary>
/// Health checks tests
/// </summary>
[TestFixture]
public class HealthChecksTests : INotificationReceiver, INotificationSender
{
    private class TestHandler : HttpClientHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Response is null)
            {
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
            return Task.FromResult<HttpResponseMessage>(Response);
        }

        public HttpResponseMessage? Response { get; set; }
    }

#pragma warning disable CS0067

    /// <inheritdoc />
    public event Func<MetadataNotification, CancellationToken, Task>? ReceiveMetadataAsync;

#pragma warning restore

    /// <inheritdoc />
    public Task SendMetadataAsync(IEnumerable<MetadataNotification> events, CancellationToken cancelToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// One time setup
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    /// <summary>
    /// Test healthy state
    /// </summary>
    /// <returns>Task</returns>
    [Test]
    public async Task TestHealthy()
    {
        // setup mock http client
        Mock<IHttpClientFactory> mockFactory = new();
        var handler = new TestHandler();
        var httpClient = new HttpClient(handler);
        handler.Response = new(System.Net.HttpStatusCode.OK);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
        HealthCheckExecutor healthCheckExecutor = new(mockFactory.Object);
        var metadata = TestUtil.CreateMetadata();

        // setup health checks
        using MetadataHealthCheckStore metadataHealthCheckStore = new(TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(250),
            new NullLogger<MetadataHealthCheckStore>());
        using MetadataStore metadataStore = new(this, metadataHealthCheckStore);
        using MetadataHealthChecker metadataHealthChecker = new(healthCheckExecutor, metadataStore, metadataHealthCheckStore,
            this, TimeSpan.FromMilliseconds(1), new NullLogger<MetadataHealthChecker>());

        // start health checking
        await metadataHealthCheckStore.StartAsync(default);
        await metadataHealthChecker.StartAsync(default);
        await metadataStore.UpsertAsync(metadata);

        // test that we are in good health
        await Task.Delay(100);
        Assert.IsEmpty(await metadataHealthCheckStore.GetHealthAsync(metadata));
        var healthChecked = await metadataStore.GetHealthyMetadatasAsync(false);
        Assert.AreEqual(1, healthChecked.Count);

        // test that we transition to bad health
        handler.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError) { Content = new StringContent("Ruh roh", Encoding.UTF8, "text/plain") };
        await Task.Delay(100);
        var health = await metadataHealthCheckStore.GetHealthAsync(metadata);
        healthChecked = await metadataStore.GetHealthyMetadatasAsync(false);
        Assert.AreEqual("Ruh roh", health);
        Assert.AreEqual(0, healthChecked.Count);

        // test that we stay in bad health
        await Task.Delay(100);
        health = await metadataHealthCheckStore.GetHealthAsync(metadata);
        healthChecked = await metadataStore.GetHealthyMetadatasAsync(false);
        Assert.AreEqual("Ruh roh", health);
        Assert.AreEqual(0, healthChecked.Count);

        // test that we recover from bad health
        handler.Response = new(System.Net.HttpStatusCode.OK);
        await Task.Delay(100);
        health = await metadataHealthCheckStore.GetHealthAsync(metadata);
        healthChecked = await metadataStore.GetHealthyMetadatasAsync(false);
        Assert.IsEmpty(await metadataHealthCheckStore.GetHealthAsync(metadata));
        Assert.AreEqual(1, healthChecked.Count);

        // test metadata drops out if no health checks
        await metadataStore.RemoveAsync(metadata);
        await Task.Delay(300);
        health = await metadataHealthCheckStore.GetHealthAsync(metadata);
        Assert.IsNull(health);
        healthChecked = await metadataStore.GetHealthyMetadatasAsync(false);
        Assert.AreEqual(0, healthChecked.Count);
    }
}
