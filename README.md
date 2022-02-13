# Service Spy - Dead Simple Service Discovery #

[![Github Sponsorship](.github/github_sponsor_btn.svg)](https://github.com/sponsors/jjxtra)

Service spy aims to be a decentralized (not blockchain) service discovery framework. Each service acts as a node and repository for service metadata, mirrored to each service.

If you want a centralized service discovery framework, you can use ZooKeeper, Consul, Eureka or the like.

By default, UDP is used and each service that registers will broadcast service metadata to the local network on a timer.

## Overview ##

TODO: Implement all of these sections
- Metadata store
- Metadata health check store
- Implementing custom metadata and metadata health check stores (needs abstraction of storage from interface to the above stores)
- UDP send/receive
- Implementing custom send/receive notifications (WAN/Internet scope)
- Making api calls
- Example projects (api service, health check service)
