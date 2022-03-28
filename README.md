# Service Spy - Simple Decentralized and Distributed Service Discovery #

[![Github Sponsorship](.github/github_sponsor_btn.svg)](https://github.com/sponsors/jjxtra)

Service spy aims to be a decentralized (not blockchain) service discovery framework. Each service acts as a node and repository for service metadata, mirrored to each service.

If you want a centralized service discovery framework, you can use ZooKeeper, Consul, Eureka or the like.

By default, UDP is used and each service that registers will broadcast service metadata to the local network on a timer.

## Metadata store
Service spy stores service metadata in memory on each service.

Service metadata is defined in appsettings.json as follows:
```
"ServiceSpy":
{
    "Services":
    {
        "Storage": "InMemory", // only InMemory supported for now
        "Items":
        [
            {
                "Id": "459F9F45-B520-42F9-C5BD-373667624231", // unique id for service
                "Name": "ServiceSpyTest", // service name
                // "Version" auto populated from entry assembly
                "Group": "USEastAZ1", // can be a region, zone, etc.
                "IPAddressString": "*", // * will pick any ip
                "Port": 7172, // port
                "Host": "127.0.0.1", // http host
                "Path": "/", // http root path
                "HealthCheckPath": "/health-check" // http health check path
            }
        ]
    }
}
```

Typically only one service would be hosted per process, but you could define multiple services in `Items` if desired. These services will be broadcast on a regular interval.

## Metadata health check store
Health check status is also stored in memory on each service. Any service performing health checks will broadcast health check results regularly.

Health checks can be setup to be performed from each service to every other service, or from dedicated health check services.

Health checks are defined in appsettings.json as follows:

```
"ServiceSpy":
{
    "HealthChecks":
    {
        "HealthCheckInterval": 0, // interval in seconds to perform health checks or 0 to not perform health checks (ignored if PerformHealthChecks is false)
        "HealthyCacheTime": 5, // cache healthy results for 5 seconds
        "CleanupInterval": 5, // interval in seconds to check for expired health check metadata
        "ExpireTime": 300, // interval in seconds to purge expired metadata that has not had a health check result, these metadata will not be considered for future service calls
        "Storage": "InMemory" // only InMemory supported for now
    }
}
```

If HealthCheckInterval is greater than 0, health checks will be performed for every distinct service metadata received that has a health check path.

## Send/receive service metadata and health checks
Service metadata and health check results are broadcast on regular intervals.

Each service configures these notifications in appsettings.json:

```
"ServiceSpy":
{
    "Notifications":
    {
        "BroadcastInterval": 5, // send service metadata notifications every 5 seconds
        "Connection":
        {
            "IPAddress": "127.0.0.1", // * to auto-select
            "Port": 7777,
            "Protocol": "Udp" // Only udp is supported currently
        }
    }
}
```

To create a health check only service, set the `BroadcastInterval` to 0 and ensure `HealthCheckInterval` is set greater than 0 in the `HealthChecks` section.

## Api calls
You can grab healthy service metadata by injecting `IMetadataStore` and calling `GetHealthyMetadatasAsync`. You will receive back 0 or more healthy metadatas and can loop over them until a successful call is made.

## Example projects
Please reference `ServiceSpy.Example.ApiService.csproj` and `ServiceSpy.Example.HealthChecks.csproj` for example code and configuration.
