namespace ServiceSpy.Notifications;

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
    /// Health check path
    /// </summary>
    public string HealthCheckPath { get; init; } = string.Empty;

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
}
