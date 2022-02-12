namespace ServiceSpy.Tests;

/// <summary>
/// Tests converting service metadata to/from binary
/// </summary>
[TestFixture]
public sealed class ServiceMetadataBinaryTests
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
    public void TestBinaryConversions(bool doDeletion, string? doHealthCheck)
    {
        var metadata = TestUtil.CreateMetadata();
        MemoryStream ms = new();
        metadata.ToBinary(ms, doDeletion, doHealthCheck);
        ms.Position = 0;
        var readMetadata = ServiceMetadata.FromBinary(ms, out bool deletion, out string? healthCheck);

        Assert.IsNotNull(readMetadata);
        Assert.AreEqual(doDeletion, deletion);
        Assert.AreEqual(doHealthCheck, healthCheck);
        Assert.IsTrue(metadata.EqualsExactly(readMetadata!));
    }
}
