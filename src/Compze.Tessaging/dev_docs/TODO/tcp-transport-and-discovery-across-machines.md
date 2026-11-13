# A TCP transport and discovery across machines — todo (investigated, nothing built)

## Status

Investigated 2026-11-13 by reading the code, plus two measurements. Nothing is built and nothing is decided:
the decisions are Magnus's, and they are listed under "Open decisions". This document records what the
investigation found, so the work can start from it instead of from a second investigation.

## Why

Deskmancer is going to run its specifications from a process on a Hyper-V host against endpoints inside a
guest: the Deskmancer worker's observation endpoint and a test driver's endpoint. Host and guest share a
network that reaches nothing else. Today the same conversation runs on one machine, over the named-pipe
transport and `InterprocessEndpointRegistry` (see
[same-machine hosting](../../../Compze.Hosting/dev_docs/wip/same-machine-hosting.md)).

- The endpoints are best-effort endpoints: observed tevents (`ObserveTevents`, `RequirePeers`), tueries and
  tommands.
- The conversation goes both ways. The worker requires the observer and publishes tevents to it; the observer
  sends tueries to the worker. Every endpoint runs a transport server, so each machine must accept connections
  from the other.
- The worker is a desktop application whose shipped form must not need the ASP.NET Core runtime.

Two things are missing: a transport that crosses machines without a web stack, and a registry that crosses
machines.

## What exists today

- **The named-pipe transport** (`Compze.Tessaging._private.Transport.NamedPipes`; 662 lines in 8 files, its
  registrars included): length-prefixed framing over a stream, one request frame answered by one response
  frame. Both ends create their pipes `PipeOptions.CurrentUserOnly`. An address is
  `compze.pipe://localhost/<pipe name>`, the name fresh for every server start.
- **The HTTP transport.** Its client (`HttpEndpointTransportClient`) lives in `Compze.Tessaging` and needs only
  the base runtime. Its server (`AspNetCoreEndpointTransportServer`, in `Compze.Tessaging.Transport.AspNetCore`)
  needs the `Microsoft.AspNetCore.App` shared framework, listens on `http://127.0.0.1:0` and announces that
  address.
- **`InterprocessEndpointRegistry`** is the only `IEndpointRegistry`. It is a memory-mapped file, and it lists
  only the addresses whose announcing process still runs (`ProcessIdentity.IsCurrentlyRunning`): a check that
  works on one machine only.
- **A transport is one seam.** It is an `IEndpointTransportClient` (send one `TransportRequest` to an
  `EndpointAddress`, get the response payload back) and an `IEndpointTransportServer` (listen at one address,
  dispatch through the endpoint's `TransportRequestHandlerMap`). Everything above the seam is the same for
  every transport: the router, the delivery streams, Typermedia.

## Piece 1: the TCP transport

### Most of the named-pipe transport needs only a stream

Call the part of the named-pipe transport that needs nothing but a duplex `Stream` the
**framed-stream-transport core**. A TCP transport is that core, plus a way to listen and a way to connect.

| Part of the named-pipe transport | Serves TCP as it is? |
|---|---|
| `NamedPipeFraming`, `NamedPipeTransportResponse` | Yes: it already takes `Stream`. It needs a name that doesn't say pipe, and a frame-size limit. |
| `NamedPipeTransportServer.ServeConnectionAsync`: the request loop and the error responses | Yes in substance. |
| `NamedPipeClientConnectionPool`: the slots, the leasing, the discarding on failure | Yes, except `OpenConnectionAsync`. |
| `NamedPipeEndpointTransportClient`, `NamedPipeEndpointTransportServer` | Yes, except that they construct pipe types. |
| The listening instances and `WaitForConnectionAsync`, `NamedPipeClientStream`, `CurrentUserOnly`, `NamedPipeAddress`, the registrars | Specific to pipes. |

So the work starts by giving the core truthful names and a home of its own, with no change in behavior (the
existing NamedPipes legs of the matrix guard it), and then adds TCP as its second carrier. Copying the six
files instead would implement the same abstraction twice.

### What is new for TCP

- **The address it listens on and announces.** Listen on one named local IP address, the machine's address on
  the shared network, never on all interfaces, with a port the operating system picks. The server's `Address`
  is then `compze.tcp://<that address>:<port>`, and it is announced as it is.
  - The announced address must equal the server's `Address`, because the router removes its own address from
    the registry's list by value (`TessagingRouter.ReconcileConnectionsAsync`). If they differ, the endpoint
    connects to itself, and the assertion that a peer never answers with this endpoint's own identity fails on
    every pass.
- **Nothing replaces `CurrentUserOnly`.** TCP knows no caller identity. In the lab the protection is the
  isolated network and listening on one address only. What would be missing for production, named and not
  designed: peer authentication and authorization, encryption and integrity, a frame-size limit
  (`NamedPipeFraming.ReadStringAsync` allocates whatever the length prefix says), and a registry that only
  trusted parties can write to.
- **A lost connection shows differently**, see below.

### How a lost connection shows

Call a peer machine that disappears without closing its connections a **silent-vanish**: the virtual machine
is paused, saved or turned off, or the network is cut.

| Event | Named pipes today | TCP |
|---|---|---|
| The peer process ends while its machine stays up. This includes a sign-out. | The kernel breaks the pipe at once. | The same signal, but an idle pooled connection shows it only when it is next used. The pool must check an idle connection before leasing it, and empty itself for that address when an exchange fails. |
| Silent-vanish | Cannot happen. | Nothing arrives. The pool has no exchange timeout, only its 10-second waits for a free connection and for a connect, so an exchange would wait indefinitely. TCP keepalive on every connection, at both ends, bounds it without bounding how long a handler may run. |
| Nothing listens at the address. | `TimeoutException` after 10 seconds. | Refused quickly if the machine is up, otherwise the operating system's connect timeout: an explicit one is needed. A TCP connect is asynchronous, so it holds no thread. |

### What is hard

- **Connection death.** The kernel gives it to pipes for free. On TCP it is ours to handle, as above.
- **The reconciliation pass connects to one address after another, with no cancellation**
  (`TessagingRouter.ReconcileConnectionsAsync`, `TessagingConnection.InitAsync`). One unreachable address
  delays the rest of the pass, and the endpoint's stop, by the connect timeout.
- **Small writes.** The framing writes each field with a call of its own. On a socket that needs one buffered
  write per frame (the framing already flushes once per frame) and Nagle's algorithm turned off, or an exchange
  may gain tens of milliseconds. Not measured.
- **Specifying it.** The matrix runs on loopback, where none of the above happens. A peer process's death can be
  specified with the endpoint host process (`test/Compze.Tests.SameMachine.EndpointHostProcess`). A
  silent-vanish cannot be produced on one machine without filtering packets, so only Deskmancer's Hyper-V runs
  would exercise it.

### Size (estimates)

- About 7 types renamed or generalized, with no change in behavior.
- About 6 types added, 350–500 lines: the address, the listener, the connection opener, the public registrar
  with its `EndpointBuilder` extension, the internal client registrar, and a value type for the address to
  listen on.
- No new package and no new dependency: sockets are in the base runtime, so it lives in `Compze.Tessaging`
  beside the named-pipe transport.

### Joining the Transport axis of the specification matrix

- `Tcp` in the `Transport` enum (`Compze.Hosting.Testing`), and a case in each of the two switches:
  `CurrentTestsEndpointTransport` and `CurrentTestsEndpointTransportClient`.
- On the matrix it runs on loopback with the testing host's `InterprocessEndpointRegistry` unchanged. The
  registry stores address URIs and doesn't care which transport they belong to, so **the transport can be built
  and proven on one machine before any registry across machines exists.**
- Lines in the `.defaults` and `.example` combination files, and `Tcp` in both matrices of `ci.yml`. At the full
  cross product that is 40 to 60 Linux jobs and 16 to 24 Windows jobs.
- The endpoint host process hard-codes `NamedPipeEndpointTransport()`, and the three SameMachine specifications
  skip AspNetCore. Passing the transport as an argument would run the multi-process specifications on TCP too,
  which is where a second process's death can be specified.

## Piece 2: what the HTTP transport would need across machines

Besides the address it listens on:

- **The announced address.** With a wildcard bind Kestrel reports `http://0.0.0.0:port` or `http://[::]:port`,
  which no other machine can use. Listening on one named IP address solves both. (Known Kestrel behavior, not
  run in the investigation.)
- **The hard-coded Seq address** (`http://192.168.0.11:5341`, tagged `todo:urgent` in
  `AspNetCoreEndpointTransportServer`) is unreachable from the guest.
- **Timeouts.** `HttpClientFactoryCE` sets neither `Timeout` nor `ConnectTimeout`, so .NET's default of 100
  seconds bounds both a silent-vanish and every handler's run time.
- **The proxy.** `SocketsHttpHandler` uses the system proxy unless told otherwise. Whether that affects requests
  to the guest depends on the host's settings, which were not checked.

Size: 3 types changed (`AspNetCoreEndpointTransportServer`, `AspNetCoreEndpointTransportRegistrar`,
`HttpClientFactoryCE`) and 1 added, under 100 lines. But a process that serves over it needs
`Microsoft.AspNetCore.App`, so the worker would either ship with it or be tested in a form that doesn't ship.

**Considered and set aside: an HTTP server on `System.Net.HttpListener`,** which is in the base runtime. It
would keep the HTTP wire, with the existing client unchanged, for about one new server type. On Windows it sits
on http.sys, where listening on anything but localhost needs a URL reservation made by an administrator, and it
gets no new features from Microsoft. It also brings none of ASP.NET Core's pipeline. Not verified by running.

## Piece 3: discovery across machines

### What the router's reconciliation needs from a registry

- **A cheap read from memory.** `ServerEndpointAddresses` is read synchronously on every pass, at least once a
  second, from async code. It must never do network I/O.
- **Liveness is entirely the registry's job.** The router never drops a connection because sends fail. An
  address that stays listed keeps its routes: `HasLiveConnectionTo` answers true, request/response sends go to
  the address and fail at the wire instead of waiting within the handler-availability patience, and
  `AwaitReadinessAsync` reports ready.
- **One address per `EndpointId`,** replaced when the endpoint announces again. An address that leaves, and a new
  one that appears, is how the router sees a restart and fetches the advertisement afresh.
- **A change signal is optional.** `AwaitPossibleMembershipChange` defaults to waiting out its timeout, which
  the router sets to one second.
- **Announcing must not do network I/O inline.** `AnnounceEndpointAddress` and `RetractEndpointAddress` are
  called once each, synchronously, inside the endpoint's start and stop, and an exception there fails the start.
  A network announcer has to keep the announcement up in the background.

### Forms the registry could take

| Form | What is configured | How liveness is known | Size (estimate) | Main cost |
|---|---|---|---|---|
| A fixed address list | Every endpoint's address, on the other machine | It isn't | 1 type, about 20 lines | Fails the sign-out case, below |
| Fixed addresses, each listed while a watch connection to it stays open (TCP only) | The same | Exactly for a process's death; by keepalive for a silent-vanish | 2 types, 150–250 lines | Fixed ports. It gives up "addresses are ephemeral and never configured", the one design rule of same-machine hosting. |
| A registry server, where an announcement lives as long as the announcer's connection to it | Only the registry server's address | The same | 4–5 types, 400–600 lines | The registry server's process must be up for anyone to find anyone |
| Broadcast beacons that expire | Nothing | It lags by the expiry time | 3 types, 250–350 lines | A second protocol, UDP firewall rules, uncertain on CI runners |

Sharing `InterprocessEndpointRegistry`'s memory-mapped file is not a form: its liveness check and its
synchronization are per machine.

**The fixed address list is the simple version, and it fails the sign-out case.** After the guest signs out,
the worker's address stays listed, so its route stays. The observer's tueries then fail at the wire at once
instead of waiting, `AwaitReadinessAsync` reports ready for a worker that is gone, and a restart at the same
address never fetches the advertisement afresh.

**The registry server form is the recommended one.** It is `InterprocessEndpointRegistry`'s own concept,
"addresses whose announcing process is no longer running are invisible", said over a network:

- **It sees that a process on the other machine ended,** because the operating system closes that process's
  connection. Keepalive covers a silent-vanish.
- **A sign-out removes every entry of the guest at once.** The router drops the connections, and sends wait
  within the handler-availability patience. Its default is 30 seconds, likely shorter than a sign-in, so the
  specifications would declare a longer one.
- **After the next sign-in the new processes announce new addresses,** which replace the old ones by
  `EndpointId`. Tevents the worker published meanwhile are held by `RequirePeers` until first contact.
- The specification process on the host is the natural home of the registry server. The guest's endpoints start
  before it exists, so their announcers must keep retrying.

### An alternative: let the router see liveness itself

The forms above keep the model in which the registry answers "who is up, and where". The alternative is to
move "who is up" to where it can be observed: the connection. The router would hold a connection to each peer
and drop the peer's routes when that connection ends, as it does today when an address leaves the registry,
and connect again while the address stays listed, as it does today for a failed connect.

- A fixed address list would then be a correct registry, and a registry server could be a plain address book.
- It needs a way for a transport client to report that a peer is gone. Named pipes and TCP can say so at once;
  HTTP could only probe.
- It changes what `IEndpointRegistry` means, and it touches the router's delivery machinery, so it must be
  walked through the exactly-once in-order guarantee before anything is built.

### The suite boundary

Today the registry's name and directory are the suite boundary, which is what lets Deskmancer's fixed
`EndpointId`s coexist across concurrent runs. Across machines the boundary becomes the registry server's
address, or the configured address list. That matters if several guests run at once.

## TCP compared with HTTP

### Measured

Both measured 2026-11-13 with `RemoteTueryPerformanceTests`: a Debug build, loopback, a tuery with an empty
result, five alternating rounds with the machine otherwise at 4–10% load. Named pipes stand in for TCP, which
was not measured.

| | HTTP | Named pipes |
|---|---|---|
| One tuery after another, per tuery | about 0.31 ms | about 0.21 ms |
| 100 tueries at once, per tuery | about 0.03 ms | about 0.03 ms |
| The specification process's peak working set | about 144 MB | about 111 MB |

- So the framed protocol answers a single round trip about 1.5 times faster, and a burst no faster.
- Most of a round trip is spent above the transport, in both. A Debug build slows Compze's own code more than
  Kestrel's, and a real payload shrinks the transport's share further.
- To repeat it, run the performance specifications' executable with `-class` naming the specification class and
  `-showLiveOutput`, with `GITHUB_ACTIONS=true` set, which makes the fixture send the timing summaries to the
  test output. Change the copy of `TestUsingPluggableComponentCombinations` beside the executable to choose the
  transport.

### What each is good for

- **TCP would be the protocol Compze already owns, on a second carrier.** The framing, the pool and the serving
  loop exist and are maintained for named pipes, which desktop suites need whatever is decided here. What TCP
  adds to maintain is the listener, the connection opener and the handling of connection death.
- **Under the transport seam as it is, TCP can do nothing HTTP cannot.** The seam is one request answered by
  one response, so every transport is used as that. What TCP has over HTTP there: no ASP.NET Core in the
  process, the faster single round trip, and a connection whose end tells its holder at once that the peer
  process ended, which is what a registry across machines needs.
- **The HTTP transport uses almost nothing of what HTTP offers.** It is seven POST routes with custom headers
  and a string body, with no middleware, no authentication and no encryption, and a tuery sent as a POST can't
  be cached by anything. What HTTP has that a protocol of our own never will is reach: browsers, clients not
  written in .NET, proxies, gateways, the web's caches, platforms that route only HTTP, and tools such as curl.
  All of that concerns parties outside the conversation between two endpoints.
- **A pipeline belongs above the seam, where it serves every transport.** Every server dispatches through
  `TransportRequestHandlerMap`, and every send goes through `IEndpointTransportClient`. Authorization by tessage
  type, tracing and metrics placed there see tessages, where ASP.NET Core middleware would see seven routes.
- **Security on TCP is small to wire and long to harden.** Encryption and certificate authentication of both
  ends are `SslStream` around the connection, about 100–200 lines; Kestrel uses the same class. Handing out and
  renewing the certificates is the larger work, under HTTP too. What ASP.NET Core has and TCP would not: years
  of hardening against misbehaving peers, and its ready-made authentication handlers.
- **Where a protocol of our own would be worth being proud of** is a connection-oriented one, which this todo
  does not include: the connection as the peer relationship, both directions over one connection so that only
  one side must accept connections, and delivery streams sent ahead with acknowledgements instead of one round
  trip per tessage. That needs a different transport seam.

## Open decisions

1. **TCP or HTTP.** With HTTP the worker either ships with ASP.NET Core or is tested in a form that doesn't ship.
2. **Extract the framed-stream-transport core, or copy the six files.** Extracting is recommended, renames first.
3. **How a process learns its address on the shared network:** a literal, a subnet to match, or an adapter's name.
4. **Ports.** Ports the operating system picks keep the design rule and need firewall rules per program; fixed
   ports allow rules per port.
5. **Which registry form, and which process hosts a registry server.** Or the alternative: the router sees
   liveness itself.
6. **The suite boundary across machines.**
7. **The TCP leg's share of the CI matrix:** the full cross product or a reduced set.

## Recommended order of work

1. Give the framed-stream-transport core truthful names and a home of its own. No change in behavior.
2. The TCP transport on loopback, as a third leg of the matrix, with the multi-process specifications running
   on it and specifications for a peer process's death.
3. Listening on a named address, keepalive and the connect timeout.
4. The registry across machines, in the form decided.

## Not verified

- No timing of TCP itself: connect and retransmission timeouts, keepalive detection, the cost of small writes.
- The .NET behaviors named above (Kestrel's reported address, the HTTP defaults, `HttpListener`'s URL
  reservations, the socket options) are from knowledge, not run.
- How the lab assigns addresses, and whether the worker and the test driver converse inside the guest. If they
  do, they need the local registry too, and an endpoint discovers through exactly one registry.
- Whether a specification exists that kills a peer process in the middle of a conversation. The harness can
  kill the process (`EndpointHostProcessHandle`).

## Noticed while investigating, not changed

- `NamedPipeEndpointTransportClient` keeps one `NamedPipeClientConnectionPool` per peer address until it is
  disposed. A restarted peer has a new address, so the pools of addresses that are gone accumulate, each with
  up to 8 dead connections.
