using System.Net;

using ServiceSpy.Notifications.Udp;

namespace ServiceSpy.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add udp send/receive capabilities for service discovery.
    /// </summary>
    /// <param name="builder">Web application builder</param>
    /// <param name="configPath">Config key/path, default is ServiceSpy:Udp</param>
    /// <returns>Services</returns>
    public static IServiceCollection AddServiceSpyUdp(this WebApplicationBuilder builder, string configPath = "ServiceSpy")
    {
        /*
        var udpConfig = new ServiceSpyUdpConfiguration();
        builder.Configuration.Bind(configPath, udpConfig);
        IPEndPoint endPoint = new(System.Net.IPAddress.Parse(udpConfig.IPAddress), udpConfig.Port);
        builder.Services.AddSingleton<INotificationReceiver>(provider => new UdpNotificationReceiver(endPoint, provider.GetRequiredService<ILogger<UdpNotificationReceiver>>()));
        builder.Services.AddSingleton<INotificationSender>(provider => new UdpNotificationSender(endPoint, provider.GetRequiredService<ILogger<UdpNotificationSender>>()));
        */
        return builder.Services;
    }
}
