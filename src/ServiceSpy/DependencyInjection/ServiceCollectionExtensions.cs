using System.Net;

using ServiceSpy.Notifications.Udp;

namespace ServiceSpy.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Udp send/receive configuration
    /// </summary>
    internal class ServiceSpyUdpConfiguration
    {
        /// <summary>
        /// IP address to bind to, empty string or * for auto
        /// </summary>
        public string IPAddress { get; set; } = string.Empty;

        /// <summary>
        /// Port to send/receive on, default 52661
        /// </summary>
        public int Port { get; set; } = 52661;

        /// <summary>
        /// Whether to broadcast service info or only receive it
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Services
        /// </summary>
        public ServiceMetadata[] Services { get; set; } = Array.Empty<ServiceMetadata>();
    }

    /// <summary>
    /// Add udp send/receive capabilities for service discovery.
    /// </summary>
    /// <param name="builder">Web application builder</param>
    /// <param name="configPath">Config key/path, default is ServiceSpy:Udp</param>
    /// <returns>Services</returns>
    public static IServiceCollection AddServiceSpyUdp(this WebApplicationBuilder builder, string configPath = "ServiceSpy:Udp")
    {
        var udpConfig = new ServiceSpyUdpConfiguration();
        builder.Configuration.Bind(configPath, udpConfig);
        IPEndPoint endPoint = new(System.Net.IPAddress.Parse(udpConfig.IPAddress), udpConfig.Port);
        builder.Services.AddSingleton<INotificationReceiver>(provider => new UdpNotificationReceiver(endPoint, provider.GetRequiredService<ILogger<UdpNotificationReceiver>>()));
        builder.Services.AddSingleton<INotificationSender>(provider => new UdpNotificationSender(endPoint, provider.GetRequiredService<ILogger<UdpNotificationSender>>()));
        return builder.Services;
    }
}
