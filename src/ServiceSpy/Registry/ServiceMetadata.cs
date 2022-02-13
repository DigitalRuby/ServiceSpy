namespace ServiceSpy.Registry;

/// <summary>
/// Represents metadata for a service
/// </summary>
public sealed class ServiceMetadata
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Group - this would usually represent an availability zone, region or general geo location
    /// </summary>
    public string Group { get; init; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public System.Net.IPAddress IPAddress { get; init; } = System.Net.IPAddress.Any;

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; init; } = 443;

    /// <summary>
    /// Host, for http(s) goes in the host header
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Path
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Health check path, including leading slash
    /// </summary>
    public string HealthCheckPath { get; init; } = string.Empty;

    /// <summary>
    /// Check if this service metadata fully equals another service metadata
    /// </summary>
    /// <param name="m">Other service metadata</param>
    /// <returns>True if fully equal, false otherwise</returns>
    public bool EqualsExactly(ServiceMetadata? m)
    {
        return m is not null &&
            this.Id == m.Id &&
            this.Name == m.Name &&
            this.Version == m.Version &&
            this.Group == m.Group &&
            this.IPAddress.Equals(m.IPAddress) &&
            this.Port == m.Port &&
            this.Host == m.Host &&
            this.Path == m.Path &&
            this.HealthCheckPath == m.HealthCheckPath;
    }

    /// <inheritdoc />
    public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
    {
        if (obj is not ServiceMetadata otherServiceMetadata)
        {
            return false;
        }

        return Id == otherServiceMetadata.Id &&
            IPAddress.Equals(otherServiceMetadata.IPAddress) &&
            Port == otherServiceMetadata.Port;
    }

    /// <inheritdoc />
    public static bool operator ==(ServiceMetadata lhs, ServiceMetadata rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
            {
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    /// <inheritdoc />
    public static bool operator !=(ServiceMetadata lhs, ServiceMetadata rhs) => !(lhs == rhs);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode() ^ IPAddress.GetHashCode() ^ Port;
    }

    /// <summary>
    /// Get service metadata from binary data
    /// </summary>
    /// <param name="s">Binary stream</param>
    /// <param name="deletion">Whether the metadata has a deletion flag</param>
    /// <param name="healthCheck">Health check</param>
    /// <returns>Service metadata or null if invalid binary data</returns>
    public static ServiceMetadata? FromBinary(Stream s, out bool deletion, out string? healthCheck)
    {
        BinaryReader reader = new(s, Encoding.UTF8);
        var serviceSpyGuidLength = reader.Read7BitEncodedInt();
        deletion = false;
        healthCheck = null;

        if (serviceSpyGuidLength == 16)
        {
            var serviceSpyGuidBytes = reader.ReadBytes(serviceSpyGuidLength);
            if (serviceSpyGuidBytes.SequenceEqual(Notifications.Udp.UdpNotificationSender.serviceSpyServiceMetadataGuid))
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
                        deletion = reader.ReadBoolean(); // is this a deletion?
                        healthCheck = reader.ReadString(); // health check
                        healthCheck = (healthCheck == "!" ? null : healthCheck);

                        var group = reader.ReadString();
                        var ipBytesLength = reader.Read7BitEncodedInt(); // ip address byte length

                        // valid ip address byte length
                        if (ipBytesLength == 4 || ipBytesLength == 16)
                        {
                            // ip bytes
                            var ipBytes = reader.ReadBytes(ipBytesLength);
                            System.Net.IPAddress ip = new(ipBytes);

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
                                                Group = group,
                                                IPAddress = ip,
                                                Port = port,
                                                Host = host,
                                                Path = path,
                                                HealthCheckPath = healthCheckPath
                                            };

                                            return newMetadata;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Write service metadata to binary stream
    /// </summary>
    /// <param name="s">Stream</param>
    /// <param name="deletion">Whether to write deletion flag</param>
    /// <param name="healthCheck">Health check</param>
    public void ToBinary(Stream s, bool deletion = false, string? healthCheck = null)
    {
        BinaryWriter writer = new(s, Encoding.UTF8);
        writer.Write7BitEncodedInt(Notifications.Udp.UdpNotificationSender.serviceSpyServiceMetadataGuid.Length);
        writer.Write(Notifications.Udp.UdpNotificationSender.serviceSpyServiceMetadataGuid);
        writer.Write7BitEncodedInt(1); // version
        var guidBytes = Id.ToByteArray();
        writer.Write7BitEncodedInt(guidBytes.Length); // id length
        writer.Write(guidBytes); // id
        writer.Write(Name); // name
        writer.Write(Version); // service version
        writer.Write(deletion); // is this a deletion?
        writer.Write(healthCheck is null ? "!" : healthCheck);
        writer.Write(Group); // group
        var ipBytes = IPAddress.GetAddressBytes();
        writer.Write7BitEncodedInt(ipBytes.Length); // ip address byte length
        writer.Write(ipBytes); // ip address bytes
        writer.Write(Port); // port
        writer.Write(Host); // host length + host bytes
        writer.Write(Path); // path length + path bytes
        writer.Write(HealthCheckPath); // health check path length + health check path bytes
    }
}
