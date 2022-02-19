using System.Net;

using ServiceSpy.HealthChecks;
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
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="configPath">Config key/path, default is ServiceSpy:Udp</param>
    /// <returns>Services</returns>
    /// <remarks><para>- If you have a protocol other than udp, you will need to add INotificationReceiver and INotificationSender
    /// with your own implementations.</para>
    /// <para>- If you are using something other than InMemory storage for metadata and health checks,
    /// you will need to register your own implementations of IMetadataStore and IHealthCheckMetadataStore.</para>
    /// <para>You can grab ServiceSpyConfiguration from the provider.</para></remarks>
    public static IServiceCollection AddServiceSpy(this IServiceCollection services,
        IConfiguration configuration,
        string configPath = "ServiceSpy")
    {
        var serviceSpyConfig = new ServiceSpyConfiguration();
        configuration.Bind(configPath, serviceSpyConfig);

        // add configuration
        services.AddSingleton<ServiceSpyConfiguration>(serviceSpyConfig);

        // add service storage
        if (serviceSpyConfig.Services.Storage.Equals("inmemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMetadataStore>(provider => new MetadataStore(provider.GetRequiredService<INotificationReceiver>(),
                provider.GetRequiredService<IMetadataHealthCheckStore>(),
                TimeSpan.FromSeconds(serviceSpyConfig.HealthChecks.HealthyCacheTime)));
        }

        // add health check storage
        if (serviceSpyConfig.HealthChecks.Storage.Equals("inmemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMetadataHealthCheckStore>(provider => new MetadataHealthCheckStore(TimeSpan.FromSeconds(serviceSpyConfig.HealthChecks.CleanupInterval),
                TimeSpan.FromSeconds(serviceSpyConfig.HealthChecks.CleanupInterval),
                provider.GetRequiredService<ILogger<MetadataHealthCheckStore>>()));
            services.AddHostedService<MetadataHealthCheckStore>(provider => (MetadataHealthCheckStore)provider.GetRequiredService<IMetadataHealthCheckStore>());
        }

        // add send/receive
        if (string.IsNullOrWhiteSpace(serviceSpyConfig.Notifications.Connection.Protocol) ||
            serviceSpyConfig.Notifications.Connection.Protocol.Equals("udp", StringComparison.OrdinalIgnoreCase))
        {
            // send and receive notifications
            IPEndPoint endPoint = new(serviceSpyConfig.Notifications.Connection.ParsedIPAddress, serviceSpyConfig.Notifications.Connection.Port);
            services.AddSingleton<INotificationReceiver>(provider => new UdpNotificationHandler(endPoint, provider.GetRequiredService<ILogger<UdpNotificationHandler>>()));
            services.AddSingleton<INotificationSender>(provider => (UdpNotificationHandler)provider.GetRequiredService<INotificationReceiver>());
            services.AddHostedService<UdpNotificationHandler>(provider => (UdpNotificationHandler)provider.GetRequiredService<INotificationReceiver>());
        }

        // if we are broadcasting notifications, setup the broadcast loop
        if (serviceSpyConfig.Notifications.BroadcastInterval > 0)
        {
            services.AddSingleton<IServiceRegistrationLoop>(provider => new ServiceRegistrationLoop(serviceSpyConfig.Services.Items,
                provider.GetRequiredService<INotificationSender>(),
                TimeSpan.FromSeconds(serviceSpyConfig.Notifications.BroadcastInterval),
                provider.GetRequiredService<ILogger<ServiceRegistrationLoop>>()));
            services.AddHostedService<ServiceRegistrationLoop>(provider => (ServiceRegistrationLoop)provider.GetRequiredService<IServiceRegistrationLoop>());
        }

        // perform health checks if configured
        if (serviceSpyConfig.HealthChecks.HealthCheckInterval > 0)
        {
            // health check implementation
            services.AddSingleton<IHealthCheckExecutor>(provider => new HealthCheckExecutor(provider.GetRequiredService<IHttpClientFactory>()));

            // health check loop
            services.AddSingleton<IMetadataHealthChecker>(provider => new MetadataHealthChecker(provider.GetRequiredService<IHealthCheckExecutor>(),
                provider.GetRequiredService<IMetadataStore>(),
                provider.GetRequiredService<IMetadataHealthCheckStore>(),
                provider.GetRequiredService<INotificationSender>(),
                TimeSpan.FromSeconds(serviceSpyConfig.HealthChecks.HealthCheckInterval),
                provider.GetRequiredService<ILogger<MetadataHealthChecker>>()));
            services.AddHostedService<MetadataHealthChecker>(provider => (MetadataHealthChecker)provider.GetRequiredService<IMetadataHealthChecker>());
        }
        return services;
    }
}
