namespace ServiceSpy.Tests;

/// <summary>
/// Tests converting service metadata to/from binary
/// </summary>
[TestFixture]
public class ServiceMetadataBinaryTests
{
    /// <summary>
    /// Test conversion without deletion and health check info
    /// </summary>
    [TestCase(false, null)]
    [TestCase(true, null)]
    [TestCase(false, "")]
    [TestCase(false, "Error")]
    [TestCase(true, "")]
    [TestCase(true, "Error")]
    public void TestBinaryConversions(bool doDeletion, string? doHealthCheck, string? ip = null)
    {
        var metadata = CreateMetadata();
        MemoryStream ms = new();
        metadata.ToBinary(ms, doDeletion, doHealthCheck);
        ms.Position = 0;
        var readMetadata = ServiceMetadata.FromBinary(ms, out bool deletion, out string? healthCheck);

        Assert.IsNotNull(readMetadata);
        Assert.AreEqual(doDeletion, deletion);
        Assert.AreEqual(doHealthCheck, healthCheck);
        Assert.IsTrue(metadata.EqualsExactly(readMetadata!));
    }

    internal static ServiceMetadata CreateMetadata()
    {
        return new ServiceMetadata
        {
            Id = id,
            Host = host,
            IPAddress = ipAddress,
            Name = name,
            Path = path,
            HealthCheckPath = healthCheckPath,
            Port = port,
            Version = version            
        };
    }

    private static readonly Guid id = Guid.NewGuid();
    private static readonly string host = "www.digitalruby.com";
    private static readonly System.Net.IPAddress ipAddress = System.Net.IPAddress.Parse("55.55.55.55");
    private static readonly string name = "name";
    private static readonly string path = "/path";
    private static readonly string healthCheckPath = "/health-check";
    private static readonly int port = 80;
    private static readonly string version = "42.69.1";
}
