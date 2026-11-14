# A TCP transport and discovery across machines — todo (both are built; what is left is listed)

## Status

Investigated 2026-11-13 by reading the code, plus two measurements. Since then, on the branch `tcp-protocol`:

- **Built:** the TCP transport, as a third leg of the specification matrix, running on loopback. See "Piece 1".
- **Tried across machines:** a pure client on a Hyper-V host conversed with an endpoint in a guest whose only
  .NET is the base runtime, while the guest was paused, disconnected, turned off and had its endpoint's process
  killed. See "Tried across machines".
- **Built and tried across machines (2026-11-14):** discovery across machines, as a registry server. See "What
  is built" and "Tried across machines" under "Piece 3".
- **Not built:** the parts listed under "What is left of the transport" and "What is left of the registry".
- **Decided (Magnus, 2026-11-13):** build TCP and keep HTTP beside it for now, since hosting where only HTTP is
  routed inbound is a real use of it. What is still open is listed under "Open decisions".

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

- **The named-pipe transport** (when investigated: `Compze.Tessaging._private.Transport.NamedPipes`, 662 lines
  in 8 files, its registrars included): length-prefixed framing over a stream, one request frame answered by
  one response frame. Both ends create their pipes `PipeOptions.CurrentUserOnly`. An address is
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

### Most of the named-pipe transport needs only a stream (done 2026-11-13)

A **framed-connection transport** is one whose connections are plain duplex byte streams, over which requests
and responses travel as frames, one request frame answered by one response frame: an exchange. The named-pipe
transport is one, and a TCP transport is another. What they share lives in
`Compze.Tessaging._private.Transport.FramedConnections`, and was the named-pipe transport's own until
2026-11-13:

| Type | What it is | It was |
|---|---|---|
| `ExchangeFraming`, `ExchangeResponse` | The wire format | `NamedPipeFraming`, `NamedPipeTransportResponse` |
| `FramedConnectionServer` | Serves every accepted connection: the request loop and the error responses | Most of `NamedPipeTransportServer` |
| `FramedConnectionPool` | The client's bounded pool of persistent connections to one peer | Most of `NamedPipeClientConnectionPool` |
| `FramedConnectionEndpointTransportClient`, `FramedConnectionEndpointTransportServer` | The `IEndpointTransportClient` and the `IEndpointTransportServer` | `NamedPipeEndpointTransportClient`, `NamedPipeEndpointTransportServer` |
| `IConnectionListener`, `IConnectionOpener` | What a framed-connection transport supplies of its own: how a connection is listened for, and how one is opened | New |

What is left that is specific to pipes: `NamedPipeConnectionListener` (the listening instances,
`CurrentUserOnly`), `NamedPipeConnectionOpener`, `NamedPipeAddress` and the registrars. A TCP transport is its
own listener, opener, address and registrars.

### What is new for TCP

- **The address it listens on and announces** (built: `TcpEndpointTransport(listenOn)`). Listen on one named
  local IP address, the machine's address on the shared network, never on all interfaces, with a port the
  operating system picks. The server's `Address` is then `compze.tcp://<that address>:<port>`, and it is
  announced as it is. An address that stands for every network interface is refused when the endpoint is built.
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
| The peer process ends while its machine stays up. This includes a sign-out. | The kernel breaks the pipe at once. | The same signal: an exchange in flight fails at once, and an idle pooled connection is found ended when it is next needed, and replaced. |
| Silent-vanish | Cannot happen. | Nothing arrives. The pool has no exchange timeout, only its 10-second waits for a free connection and for a connect. TCP keepalive on every connection, at both ends, bounds an exchange without bounding how long a handler may run. |
| Nothing listens at the address. | `TimeoutException` after 10 seconds. | The same 10 seconds, also when the machine is up: its firewall drops the connect instead of refusing it. A TCP connect is asynchronous, so it holds no thread. |

What each of these looks like to a conversation was observed, not only reasoned: see "Tried across machines".

### What is hard

- **Connection death.** The kernel gives it to pipes for free. On TCP it is ours to handle, as above.
- **The reconciliation pass connects to one address after another, with no cancellation**
  (`TessagingRouter.ReconcileConnectionsAsync`, `TessagingConnection.InitAsync`). One unreachable address
  delays the rest of the pass, and the endpoint's stop, by the connect timeout.
- **Small writes** (handled). The framing wrote each field with a call of its own, which on a socket is a send
  per field. A frame is now one write, and Nagle's algorithm is off. What the small writes would have cost was
  not measured.
- **Specifying it.** The matrix runs on loopback, where none of the above happens. A peer process's death can be
  specified with the endpoint host process (`test/Compze.Tests.SameMachine.EndpointHostProcess`). A
  silent-vanish cannot be produced on one machine without filtering packets: the across-machines lab produces
  it, with a person or an agent reading the outcome. See "Tried across machines".

### What is built (2026-11-13)

- **The transport**, in `Compze.Tessaging` beside the named-pipe transport, with no new package and no new
  dependency: `TcpEndpointTransport(listenOn)` (public, `Compze.Tessaging.Transport.Tcp`), and behind it
  `TcpConnectionListener`, `TcpConnectionOpener`, `TcpConnection`, `TcpAddress` and the registrars.
- **A frame is one write** (`ExchangeFraming`), for named pipes too, so that on a socket a frame is one send.
- **Every connection, at both ends, has Nagle's algorithm off and TCP keepalive on** (`TcpConnection`): a
  connection whose peer vanished is failed after about ten seconds of silence. Verified against a real
  silent-vanish in the lab.
- **A connect has a timeout of its own,** the pool's 10 seconds.
- **A request that waits in vain for a free pooled connection throws `EndpointBusyException`** (2026-11-14):
  the peer is reached, and its handlers don't keep up with what is sent to it. Until then it threw a
  `TessageDispatchingFailedException` whose message spoke of a remote exception. Specified by
  `Given_a_pure_client_whose_endpoint_is_busy`, and seen with the lab program on loopback: of nine tueries at
  once, each answered after 13 seconds, the ninth failed after 10 and the other eight were answered.
- **An idle connection that has ended is discarded, not used** (`ClientConnection.HasEndedWhileIdle`, asked by
  `FramedConnectionPool` before it uses an idle connection again). Before this, each connection that had died
  idle failed one exchange after the peer was back: 6 of 8 tueries in the lab.
- **A pure client can declare the transport's client side:** `TcpEndpointTransportClient()`, and
  `NamedPipeEndpointTransportClient()` for named pipes. Only the HTTP transport offered that before.
- **The Tcp leg of the matrix:** `Tcp` in the `Transport` enum, a case in `CurrentTestsEndpointTransport` and in
  `CurrentTestsEndpointTransportClient`, the `.example` and `.github-ci` combination files, and both matrices
  of `ci.yml`, which at the full cross product makes 60 Linux jobs (from 40) and 24 Windows jobs (from 16). On
  the matrix the transport listens on loopback, with the testing host's `InterprocessEndpointRegistry`
  unchanged: the registry stores address URIs and doesn't care which transport they belong to.
- **Specified:** the whole suite passes on `SqliteMemory:Microsoft:Newtonsoft:Tcp`, and
  `An_endpoint_declaring_the_TCP_transport` specifies the address the declaration gives an endpoint.
- **`C-Test-Combinations`** (DevScripts) runs the built tests against combinations named on the command line,
  from temporary copies, so a change can be checked on another transport without touching the local
  combinations file.

### Tried across machines (2026-11-13)

**The across-machines lab** is what tries the transport between two machines:

- **The lab machine** is a Hyper-V virtual machine on the development computer, `Compze-Win11-24H2`, in
  `C:\CompzeVMLab`: a clone of the baseline of Deskmancer's Windows 11 24H2 machine, connected to Hyper-V's
  Default Switch. Its Windows has the host's .NET runtime and no ASP.NET Core runtime. DevScripts' `C-VM-*`
  commands create it, start it from its `baseline` checkpoint, start the lab endpoint in it, and hold a
  conversation while it is made to vanish (`C-VM-ConverseWhileTheLabMachineVanishes`, which repeats the runs
  behind the table below).
- **The network between the two** is the Default Switch: the guest gets an address there that the host can
  connect to, and reaches the internet through it. Each side's firewall has a rule for the other:
  - The guest's lets in TCP from that network. `C-VM-CreateLabMachine` adds the rule.
  - The host's lets in everything that arrives through the Default Switch, since 2026-11-14: the rule "Hyper-V
    guests on the Default Switch may reach this computer", added by hand from an elevated process. It is as wide
    as Magnus wants it: "Guests have 100% network access to my workstation, any protocol, port etc." A registry
    server on the host needs it. Before it a TCP connect from the guest to the host got no answer; with it the
    guest connects in 15 ms. The rule names the switch's network adapter by its id; whether that id outlasts a
    restart of the host is not verified. `C-VM-TestLabNetwork` tells.
- **The lab program** (`test/Compze.Tests.AcrossMachines.Lab`) is both sides. `serve` hosts the lab endpoint, a
  best-effort endpoint over TCP that answers one tuery, in the lab machine. `converse` holds a conversation with
  it from the host: a pure client asks the tuery round after round and writes one line for each, answered and
  how fast, or failed, after how long and with what. A tuery orders its own answer: how large, and after how long.

**What a conversation experienced** (a Debug build, one run of each):

| What was done to the lab machine | A tuery on an open connection | A tuery that needs a new connection | Once the machine is back |
|---|---|---|---|
| Nothing | Answered in 1–2 ms | The first one: 60 ms | |
| Paused | Fails after 9.5 s: the connection ended before the answer arrived (the operating system: "connected host has failed to respond") | Fails after 10 s: no connection was made in time | The next tuery is answered in 2 ms |
| Disconnected from the network | The same | The same | The same; the guest keeps its address |
| Turned off | The same | The same | |
| The lab program killed | Fails at once: the connection ended ("forcibly closed by the remote host") | Fails after 10 s, as for a machine that is gone: the guest's firewall drops a connect to a port nothing listens at | |

Every failure in the table is an `EndpointUnreachableException` since later the same day, when the first two
rows were run again. When the table was made the failures were a raw `IOException` on an open connection and a
`TessageDispatchingFailedException` for a connect.

- **Keepalive does what it is there for.** A tuery whose answer was ordered after 40 s was in flight, its
  connection silent, when the machine was paused: it failed 6.7 s after the pause, 10.1 s after it was asked.
  Without keepalive nothing would have ended it.
- **Large answers work:** eight answers of 5 million characters at once in 60–100 ms each, and one of 100
  million characters in 0.7 s.
- **No ASP.NET Core runtime is needed:** the guest has none, and the lab endpoint runs there.

**What the lab found that the suite could not:**

- **A pure client composed as an application composes it could not connect,** over any transport: fixed, and
  the suite's pure test client is now composed that way, so it would catch it. See the changelog of
  Compze.Tessaging 0.9.0-alpha.
- **Connections that died idle each failed one exchange:** fixed, above.
- **A failure to reach the endpoint was thrown as unrelated exceptions:** fixed, with `EndpointUnreachableException`.

**What the lab is not:** a specification. Nothing asserts; someone reads the lines. See "Open decisions".

**Noticed about the lab machine itself:**

- Its first start of a program after a boot takes about 70 s, later starts a second or two.
  `C-VM-StartLabEndpoint` waits for it.
- The guest reaches the internet. One download stalled in it on 2026-11-13, around 03:15 at night, during a
  fresh clone's first boot: `https://dot.net/v1/dotnet-install.ps1`, on which `Invoke-WebRequest` waited for
  nine minutes and curl then gave up after one ("server closed abruptly"). No other site was tried then, nor
  the same download from the host, so nothing tells where it stalled. The day after it could not be made to
  stall: the same download took half a second from the baseline guest, and each of 38 times through the first
  12 minutes of a fresh clone's first boot, with curl and with `Invoke-WebRequest`, and 48 MB came in a second.
  `C-VM-TestLabNetwork` is what to run if it happens again: it downloads from the host and the guest side by side.
- The lab downloads nothing either way. Its guest gets the host's own .NET runtime, copied in, so it runs what
  the build's tests ran on, and has no ASP.NET Core runtime because none was copied: that is the design, not a
  consequence of the stall.

### What is left of the transport

- **What is left around `EndpointUnreachableException`** (built 2026-11-13: every transport's client throws it
  when no connection can be made or the connection ends before the answer arrives):
  - Over HTTP, a request that gets no response within `HttpClient`'s timeout (100 seconds, never set) ends in
    a `TaskCanceledException`, and is deliberately not caught (2026-11-14). The timeout bounds both an endpoint
    whose machine vanished while it handled the request and a handler that runs for longer, and nothing tells
    them apart when it expires, so an `EndpointUnreachableException` there would be a guess. What HTTP needs is
    what TCP has: keepalive on its connections, a connect timeout, and no timeout on the request. See "Timeouts"
    under "Piece 2". Tagged `bug:` in `HttpEndpointTransportClient`, and what to change `todo:` in
    `HttpClientFactoryCE`.
  - In the normal suite a connection that ends in the middle of an exchange is specified over named pipes only:
    over TCP only the lab can produce it, and it does.
- **Specifications of a peer process's death.** The endpoint host process hard-codes
  `NamedPipeEndpointTransport()`, and the three SameMachine specifications skip AspNetCore and Tcp. Passing the
  transport as an argument would run the multi-process specifications on TCP too.
- **The reconciliation pass's connects can't be cancelled** (see "What is hard").
- **A frame-size limit,** and the rest of what is missing for production, named under "What is new for TCP".
- **An accept that fails while the listener is not stopping** ends the accept loop: see the `review:` comment in
  `TcpConnectionListener`.

## Piece 2: what the HTTP transport would need across machines

Besides the address it listens on:

- **The announced address.** With a wildcard bind Kestrel reports `http://0.0.0.0:port` or `http://[::]:port`,
  which no other machine can use. Listening on one named IP address solves both. (Known Kestrel behavior, not
  run in the investigation.)
- **The hard-coded Seq address** (`http://192.168.0.11:5341`, tagged `todo:urgent` in
  `AspNetCoreEndpointTransportServer`) is unreachable from the guest.
- **Timeouts.** `HttpClientFactoryCE` sets neither `Timeout` nor `ConnectTimeout`, so .NET's default of 100
  seconds bounds both a silent-vanish and every handler's run time.
  - What would make HTTP behave as TCP does, on `HttpClientFactoryCE`'s `SocketsHttpHandler`: a `ConnectTimeout`
    of the pool's 10 seconds, TCP keepalive on each connection through a `ConnectCallback` (as `TcpConnection`
    turns it on), and `HttpClient.Timeout` set to infinite. About 30 lines. Tagged `todo:` in
    `HttpClientFactoryCE`, with how to verify it.
  - Run 2026-11-14 on .NET 10, against a server that accepts and never responds, and an address nothing answers
    at: both `HttpClient.Timeout` and `ConnectTimeout` end the request in a `TaskCanceledException` whose
    `InnerException` is a `TimeoutException`. Only the messages tell the two apart. A caller's own cancellation
    has no `TimeoutException` inside.
  - Not run: that keepalive ends an HTTP request to a vanished machine. The lab can't serve HTTP: its program
    serves over TCP only, and its machine has no ASP.NET Core runtime, by design. What it would take: the lab
    program serving over HTTP too, and a guest given the host's ASP.NET Core runtime for that run, copied in as
    the .NET runtime is (`C-VM-CreateLabMachine`). The baseline stays without it, so a TCP run goes on proving
    that it needs none.
  - On Windows a request written to a vanished machine fails sooner than the 100 seconds, as an
    `EndpointUnreachableException`: the operating system gives up resending it after about 10 seconds. (Reasoned
    from the TCP rows of "Tried across machines", not run over HTTP.) The 100 seconds are met when the machine
    vanishes after it received the request.
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

### What Deskmancer needs of it

Read from Deskmancer's code on 2026-11-14:

- **Deskmancer has two channels, each with a registry of its own,** two registry files in one directory that the
  specification process makes for the run:
  - the observation channel (registry `Deskmancer.ExternalObservation`): the worker's `ExternalObservationEndpoint`
    and the specification process's `ExternalObservationObserver`;
  - the real-Windows driver's channel (registry `Deskmancer.RealWindowsDriver`): the driver process's
    `RealWindowsDriverEndpoint` and the specification process's `RealWindowsDriverClient`.
- **The worker and the driver do not converse with each other.** The driver starts Deskmancer as a process, with
  launch arguments, and each of the two converses only with the specification process. `RealWindowsDriverChannel`
  says it: "the two channels share a directory and nothing else: Deskmancer's worker never meets the driver's
  tessages."
- **So nothing in the guest needs the local registry beside the one across machines:** every conversation there
  crosses to the host. An endpoint discovering through exactly one registry is enough.
- **And it needs two registries across machines that stay apart,** as the two files do today: two registry
  servers, or one that keeps registries apart by name.

### The suite boundary

Today the registry's name and directory are the suite boundary, which is what lets Deskmancer's fixed
`EndpointId`s coexist across concurrent runs. Across machines the boundary becomes the registry server's
address, or the configured address list. That matters if several guests run at once.

### What is built (2026-11-14)

Magnus chose the registry server form: "it is alpha, we are not promising api stability, and it is what you
recommend."

- **`EndpointRegistryServer` and `NetworkEndpointRegistry`,** in Compze.Hosting's `Compze.Hosting.AcrossMachines`,
  beside `InterprocessEndpointRegistry`: 2 public types and 5 private ones, about 450 lines. A process opens a
  registry on the server by name, and an endpoint participates in it as in any registry.
- **Registries of different names share nothing but the server,** which is what Deskmancer's two channels need.
- **An announcement is in the registry for as long as its announcer's connection to the server lasts.** The
  connection is set up as the TCP transport's are, keepalive included (`TcpConnection`, now shared with
  Compze.Hosting).
- **Each end sends the other its whole state whenever it changes,** never a change to it: what the process
  announces one way, the registry's membership the other. A reconnect needs nothing but to send again.
- **Without a connection to the server a registry lists nothing,** and tries to connect every second. So it can
  be opened, and announced to, before the server exists.
- **What the router's reconciliation needs** holds: membership is read from memory,
  `AwaitPossibleMembershipChange` wakes when the server tells of a change, and announcing does no network I/O
  inline.
- **Specified** by the 16 specifications in `test/Compze.Tests.Integration/AcrossMachines`: registry against
  registry through a server on loopback, the server's absence and return, and two endpoints in hosts of their
  own that find each other through it, over every transport of the matrix.

### Tried across machines (2026-11-14)

The lab program has a second way to converse, `converse-through-registry`: a conversing endpoint on the host
that is told no address. It hosts the registry server, as a specification process would, and finds the lab
endpoint through the lab registry. The lab endpoint in the guest is told only where the server will listen, and
is started before the server exists. `C-VM-ConverseWhileTheLabMachineVanishes -ThroughRegistry` runs it.

**What a conversation through the registry experienced** (a Debug build, one run of each):

| What was done to the lab machine | The host's router dropped its route to the lab endpoint | A tuery in flight | A tuery asked while it was away | Once it was back |
|---|---|---|---|---|
| Nothing | | | | Found 0.5–0.7 s after the registry server started. The first tuery 110–370 ms, then 1–2 ms |
| The lab program killed, and started again 12 s later | Within 0.8 s of the kill | | Waited, and was answered after 16.6 s, at the endpoint's new address | The next tuery in 2 ms |
| Paused for 22 s | 5.6 s after the pause | Failed after 8 s: `EndpointUnreachableException` | Waited, and was answered after 16.9 s: 6.6 s after the resume | The next tuery in 2 ms |
| Disconnected from the network for 22 s | 5.5 s after | Failed after 8 s, the same | Waited, and was answered after 12 s: 1.3 s after it was connected again | The next tuery in 2 ms |
| The lab program killed, and not started again | Within 0.8 s of the kill | | Failed after 30 s: `NoHandlerForTypermediaTypeException`, which names the handler-availability patience | |

- **The sign-out case behaves as wanted.** A tuery asked while the endpoint is away waits, where a client told
  the address failed at the wire, and it is answered by the endpoint that comes back at a new address.
- **A resumed guest takes about 6 s to rejoin:** its connection to the server ended while it was paused, which
  it learns from keepalive after 5 s of silence. It then connects again within a second.
- **A route is dropped faster than keepalive's 10 s** when the connection had already been silent for some
  seconds: the silence before the first probe counts from the last frame.

### What is left of the registry

- **Deskmancer's use of it:** an environment for its two channels that participates in a `NetworkEndpointRegistry`
  over the TCP transport, and how the guest's processes are told the server's address. The host's address on the
  Default Switch changes when the host restarts.
- **Specifications across processes.** A participant's end is specified by disposing its registry in the same
  process, and tried for real only in the lab.
- **No limit on the size of what is received, and no security,** as for the transport.
- **The server keeps its registries in memory.** One that starts again knows nothing until the participants
  have connected again, which takes them up to a second.

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

1. **How a process learns its address on the shared network.** `TcpEndpointTransport` takes the `IPAddress`
   today; what finds it is open: a literal, a subnet to match, or an adapter's name.
2. **Ports.** Ports the operating system picks keep the design rule and need firewall rules per program; fixed
   ports allow rules per port. Only picked ports are built.
3. **Which registry form:** decided and built, a registry server (Magnus, 2026-11-14). Which process hosts it is
   the application's choice; for Deskmancer the specification process is the natural one.
4. **The suite boundary across machines:** the registry's name on a registry server, as built.
5. **The shape of the CI matrix.** `Tcp` was added to the full cross product, 84 jobs from 56. The alternative
   is one transport on the full product and the others on a reduced leg that covers every persistence layer
   and every DI container once: about 46 jobs with three transports.
6. **Whether the HTTP transport stays in the long run.** Most likely it does (Magnus, 2026-11-14): the only
   major argument against it is the run time of the full matrix, which is no good argument for removing a
   protocol. So what HTTP lacks across machines ("Piece 2") is work to do, not work that may never be needed.
7. **The CA2000 suppression in `TcpConnectionOpener`:** the analyzer cannot follow a socket's ownership into
   the `NetworkStream` that closes it. The alternative is an analyzer option for the whole repository
   (`dispose_ownership_transfer_at_constructor`).
8. **Whether the across-machines lab becomes specifications.** They would run only where the lab machine
    exists, so outside `C-Test` and CI, as Deskmancer's Hyper-V matrix does. Fixed ports (decision 2) would let
    the normal suite specify an endpoint that comes back at the same address, which today only the lab can produce.

## Recommended order of what is left

1. Deskmancer's use of the TCP transport and the registry across machines: see "What is left of the registry".
2. The multi-process specifications on TCP and through a registry server, with specifications for a peer
   process's death.

## The next merge of main: main targets .NET 11 (since 2026-11-14)

Main was last merged in at fc61ab357, for the two CI changes. Since then the dotnet-11 branch is merged into main
(be4e1cefe): `src/Directory.Build.props` sets `net11.0` for every project and no project file sets a target framework,
runtime async is on, and `global.json` is in the repository's root and pins SDK 11.0.100. The dotnet-11 session is also
preparing a release of every package from main. The merge has no conflicts in text (tried 2026-11-14), but it needs
this by hand:

- **The lab program's project file** (`test/Compze.Tests.AcrossMachines.Lab`) still says
  `<TargetFramework>net10.0</TargetFramework>`: drop the line.
- **Three DevScripts commands that exist only on this branch name the output folder `bin\Debug\net10.0`:**
  `C-Test-Combinations`, `C-VM-StartLabEndpoint` and `C-VM-ConverseWhileTheLabMachineVanishes`. Better than renaming
  it to `net11.0`: have them find the folder, so the next target framework breaks nothing.
- **The lab machine has only the .NET 10 runtime,** so a lab program built for .NET 11 doesn't start in it.
  `C-VM-CreateLabMachine` copies the host's runtime of `-DotNetChannel`, which defaults to `10.0`: make it `11.0`, then
  remove the lab machine and create it again, which takes a few minutes.
- **Versions.** If main's release takes a version this branch has taken, the branch moves to the next, as it did for
  Compze.Tessaging (0.9.0-alpha here, after main released 0.8.0-alpha). Unreleased on this branch: Compze.Tessaging
  0.9.0-alpha, Compze.Tessaging.Transport.AspNetCore 0.4.0-alpha, Compze.Hosting 0.4.0-alpha, Compze.Hosting.Testing
  0.4.0-alpha and Compze.Tessaging.Hosting.Testing 0.5.2-alpha.
- **Then:** build, read the warnings (.NET 11's analyzers), run the full suite, and one lab run through the registry.

Merging once, after main's release of every package has landed, saves a second round of the versions.

## Not verified

- The cost of small writes, and how TCP's timings compare with HTTP's across machines.
- The .NET behaviors named above (Kestrel's reported address, the HTTP defaults, `HttpListener`'s URL
  reservations) are from knowledge, not run.
- How Deskmancer's Hyper-V runs will assign addresses. (Whether the worker and the test driver converse inside
  the guest is answered under "What Deskmancer needs of it": they don't.)
- Whether a specification exists that kills a peer process in the middle of a conversation. The harness can
  kill the process (`EndpointHostProcessHandle`).

## Noticed while investigating, not changed

- `FramedConnectionEndpointTransportClient` keeps one `FramedConnectionPool` per peer address until it is
  disposed. A restarted peer has a new address, so the pools of addresses that are gone accumulate, each with
  up to 8 dead connections. Tagged `bug:` at the code.
- `TcpEndpointTransportRegistrar`, `NamedPipeEndpointTransportRegistrar` and `AspNetCoreEndpointTransportRegistrar`
  each hold old-style extension methods beside an extension block, which ReSharper hints at converting.
  Converted, a class with two extension blocks trips the analyzer's CA1708, a false positive that the handler
  registrars suppress (`TessageBusHandlerRegistrarCE`). Tried and put back: the choice between that suppression
  and a class for each receiver is open.
