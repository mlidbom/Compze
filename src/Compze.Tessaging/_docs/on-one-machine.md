# Processes on one machine

An application may be several processes on one machine: say a desktop application, a background indexer and a tray
helper, each hosting endpoints that converse with the others'. Tessaging connects them with no web server, no ports or
addresses to configure, and no database server: each process declares its endpoints and names the registry the
application's processes share.

## Named pipes, and an address for each start

The named-pipe transport is part of Compze.Tessaging, built on .NET's own pipes, so a process loads no web stack for
it. An endpoint declares it in its environment with `NamedPipeEndpointTransport()`.

- **A new address each time an endpoint starts.** Addresses are never configured, nor written down anywhere: the
  endpoint's `EndpointId` is what identifies it, and discovery finds where it listens now.
- **Only the same user.** A pipe accepts connections only from processes that run as the user that created it.
- **Windows, Linux and macOS.** On Linux and macOS, .NET's pipes are Unix domain sockets.

## The interprocess registry

Endpoints announce their addresses to an `InterprocessEndpointRegistry`, and find each other's there. Its state is a
memory-mapped file, so it needs no server, and what one process announces is visible at once to the others:

- **Its name and directory make the application.** The processes that open the registry of the same name, in the same
  directory, in the same login session, find each other's endpoints. Another application opens a registry of its own.
- **`ParticipateIn(registry)` declares both sides,** finding the others through it and announcing the endpoint to it.
  `DiscoverEndpointsThrough` and `AnnounceAddressTo` declare one side each.
- **A crashed process's addresses disappear.** Each announcement records the process that made it, and an address
  whose process no longer runs is never listed.

An endpoint doesn't connect once and assume the result. It keeps its connections in line with what the registry lists
now: an endpoint that appears is connected to, one that leaves is dropped, and one that restarts at a new address is
reconnected, with what was waiting for it. An announcement wakes the other processes at once; a crash, which announces
nothing, is noticed within a second.

## An application of two processes

This application's indexer process reports its progress to its desktop process, with best-effort tevents: neither
process stores anything, so neither needs a database.

The tevents, and the identities of the two endpoints, are in an assembly both processes reference. An endpoint's
identity is a type of its own here, rather than its declaration, because the indexer refers to the desktop's endpoint,
which only the desktop process declares:

[!code-csharp[](on-one-machine.cs#shared_contracts)]

Both processes run their endpoints in the same environment:

[!code-csharp[](on-one-machine.cs#environment)]

The desktop process handles the indexer's tevents:

[!code-csharp[](on-one-machine.cs#desktop_process)]

The indexer process publishes them:

[!code-csharp[](on-one-machine.cs#indexer_process)]

The two processes start in either order. The indexer requires the desktop's endpoint, so the tevents it publishes
before the two have found each other are held for the desktop, and delivered once they have. For tommands and tueries
sent before their handlers are found, see [Readiness and waiting sends](readiness-and-waiting-sends.md).

## Exactly-once across processes

Exactly-once endpoints need a domain database, and on one machine SQLite database files do with nothing to install:
the [environment on the Endpoints page](endpoints.md#the-environment) gives each its file in a data directory. An
exactly-once endpoint runs in one process at a time, so two processes that share a domain database host different
endpoints of it.

## Across machines

Compze's transports reach endpoints on the same machine today: named pipes, and HTTP through ASP.NET Core
(Compze.Tessaging.Transport.AspNetCore), which listens on the loopback interface. Endpoints on different machines can't
converse yet.
