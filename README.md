# Service Spy - Dead Simple Service Discovery #

[![Github Sponsorship](.github/github_sponsor_btn.svg)](https://github.com/sponsors/jjxtra)

Service spy aims to be a decentralized (not blockchain) service discovery framework. Each service acts as a node and repository for service metadata, mirrored to each service.

If you want a centralized service discovery framework, you can use ZooKeeper, Consul, Eureka or the like.

By default, UDP is used and each service that registers will broadcast service metadata to the local network on a timer.

For centralized cases, or where WAN (Internet) scope is needed, you can configure Service Spy to use DNS or a centralized datastore for service metadata.
