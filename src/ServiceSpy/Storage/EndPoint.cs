using System.Diagnostics.CodeAnalysis;

namespace ServiceSpy.Storage;

/// <summary>
/// An end point for a service - this contains the ip address, port and path for base service calls
/// </summary>
public readonly struct EndPoint
{
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

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not EndPoint ep)
        {
            return false;
        }

        return IPAddress.Equals(ep.IPAddress) &&
            Port == ep.Port &&
            Host.Equals(ep.Host, StringComparison.OrdinalIgnoreCase) &&
            Path.Equals(ep.Path, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IPAddress.GetHashCode() ^ Port ^ Host.GetHashCode() ^ Path.GetHashCode();
    }
}
