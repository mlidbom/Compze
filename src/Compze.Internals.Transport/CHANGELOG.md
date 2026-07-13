# Changelog

All notable changes to Compze.Internals.Transport will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- The named-pipe transport substrate (`Compze.Internals.Transport.NamedPipes`): framed request/response conversations over `System.IO.Pipes` — base runtime only, no web stack. `NamedPipeTransportServer` serves connections through a fixed pool of listener loops (bounded concurrency, backpressure through pending connects); `NamedPipeTransportClient` sends one request per connection; handler exceptions travel back as error frames rethrown client-side as `MessageDispatchingFailedException`. Includes the named-pipe `IInfrastructureQueryTransport` and the infrastructure-query handler every pipe transport server registers.

## 0.1.0-alpha

- Initial pre-release
