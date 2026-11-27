# Endpoints

An *endpoint* is what lets tessages cross between parts of a system that don't share a container. It is the engine of
[one process](in-one-process.md) given an identity and a wire: a container of its own, the handlers its declaration
declares, and a transport server through which the other endpoints reach it. One process may host one endpoint or
several. Endpoints in the same process converse through their transport, exactly as endpoints in different processes
do, so moving an endpoint to a process of its own changes nothing about how it converses.

## Two kinds of endpoint

| | Best-effort endpoint | Exactly-once endpoint |
|---|---|---|
| Declared by inheriting | `BestEffortEndpointDeclaration<TIdentity>` | `ExactlyOnceEndpointDeclaration<TIdentity>` |
| Needs a database | No. | Yes: it joins a domain database. |
| Tevents it publishes | Local ones (`ITevent`) and best-effort ones (`IRemotableTevent`). | Every kind, exactly-once ones (`IExactlyOnceTevent`) included. |
| Tevents it receives | Best-effort ones. | Best-effort ones and exactly-once ones. |
| Tommands it sends and handles | None of the TessageBus's. | Exactly-once tommands. |
| What it remembers about other endpoints | For as long as its process runs. | In its database, across restarts. |

Both serve tueries and typermedia tommands. An exactly-once endpoint keeps an inbox and an outbox in its domain
database: they are what make its deliveries exactly-once. A best-effort endpoint asked to publish an exactly-once tevent
refuses: a guarantee is never silently weakened.

## Declaring an endpoint

An endpoint is declared as a class. These declare two endpoints that converse with each other through these tessages:

[!code-csharp[](endpoints.cs#tessages)]

The Accounts endpoint registers accounts, and records that their welcome emails were sent:

[!code-csharp[](endpoints.cs#accounts_endpoint)]

The WelcomeEmails endpoint sends a welcome email for each registered account, and tells the Accounts endpoint that it
did:

[!code-csharp[](endpoints.cs#welcome_emails_endpoint)]

### Identity

A declaration usually is its own identity: it implements `IEndpointIdentity`, whose static `Name` and `Id` it builds
under. The `Name` is for humans. The `EndpointId` is the endpoint's identity across processes and restarts, so it is a
fixed `Guid`, written once and never changed. Code that refers to another endpoint refers to its identity type, such as
`AccountsEndpoint.Id`, and the compiler checks the reference.

When several processes must refer to an endpoint that only one of them declares, its identity can be a type of its own,
in an assembly they share: see [Processes on one machine](on-one-machine.md).

An exactly-once endpoint's name prefixes its tables in the domain database, so it is a letter followed by letters,
digits and underscores, at most 28 characters long.

### What a declaration overrides

A declaration overrides only what its endpoint uses. Each kind of handler has its override, which gets a registrar
that can register only that kind, so a handler of the wrong kind can't be registered:

| Override | Registers | Endpoint |
|---|---|---|
| `RegisterExactlyOnceTommandHandlers` | handlers of exactly-once tommands | exactly-once |
| `RegisterExactlyOnceTeventHandlers` | handlers of exactly-once tevents | exactly-once |
| `RegisterBestEffortTeventHandlers` | handlers of the other tevents | both |
| `ObserveTevents` | [observers](observing-tevents.md) | both |
| `RegisterTueryHandlers`, `RegisterTypermediaTommandHandlers` | Typermedia's handlers | both |
| `RegisterComponents` | the endpoint's other components, and the type identities it needs | both |
| `RequiredPeers`, `PeersNotQueuedFor`, `HandlerAvailabilityPatience` | how it treats other endpoints: see [Delivery guarantees](delivery-guarantees.md) and [Readiness and waiting sends](readiness-and-waiting-sends.md) | both |
| `Declare` | anything else, on the endpoint's whole builder, such as a tevent store | both |

A best-effort endpoint's declaration has no overrides for exactly-once handlers: it has no database to deliver them
exactly once with. A subscription in the wrong override fails when the endpoint is built, naming the override it
belongs in.

> [!NOTE]
> An exactly-once handler's effects within its transaction happen exactly once. A side effect outside the transaction,
> such as the email above, happens again if the handler fails after it and is retried.

## Type identities

A tessage that crosses between endpoints carries the identity of its type, and that identity must survive renaming and
moving the type. So every type that crosses is mapped to a `Guid`, in a class its assembly declares:

[!code-csharp[](type-mappings.cs#type_mapper)]

Map the tommands sent, the tevent interfaces subscribed to, and the tevent classes published. Each endpoint then
declares, in `RegisterComponents`, the assemblies whose types it needs, with
`RequireMappedTypesFromAssemblyContaining<T>()`: those of the types it sends, publishes, handles and subscribes to, and
those of every type that the endpoints it converses with handle or subscribe to. An endpoint that handles or subscribes
to a type it can't map fails when it is built, naming the type.

> [!WARNING]
> An endpoint connects to another only if it can map every type the other handles and subscribes to. When it can't, it
> logs a warning at each attempt to connect, and its sends to that endpoint wait, then fail as if nothing handled their
> tessages.

## The environment

What a declaration doesn't say is where its endpoint runs: the transport, the serializer, how it finds the other
endpoints, and, for an exactly-once endpoint, the domain database it joins. An `IEndpointEnvironment` says that, once
for every endpoint of a deployment:

[!code-csharp[](endpoints.cs#environment)]

> [!NOTE]
> In an ASP.NET Core project, whose implicit usings include `Microsoft.AspNetCore.Builder`, the name `EndpointBuilder` is
> ambiguous. `using EndpointBuilder = Compze.Tessaging.Endpoints.EndpointBuilder;` resolves it.

The same declaration builds in any environment, so production and tests host the same endpoint, in different
environments.

The domain database is the domain's, not the endpoint's: any number of exactly-once endpoints join one, each with tables
of its own, and the database holds the domain's own data too, so that an endpoint's handlers change the domain and
record their tessages in one transaction. The domain database is reached through connection strings, by name, from the
container's `IConfigurationParameterProvider`. On SQLite, an endpoint whose domain database is named `Accounts` uses
three databases: `Accounts`, `Accounts.PeerRegistry` and `Accounts.TypeIdInterner`. The environment above gives each a
file of its own in a data directory.

## Hosting

An `EndpointHost` builds each registered declaration in its environment, on a new container builder from the factory
it was given, and starts and disposes them together:

[!code-csharp[](endpoints.cs#hosting)]

The sent `RegisterAccount` is handled at once, within the send's own unit of work, since its handler is in the sending
endpoint; the `AccountRegistered` that its handler publishes crosses to the WelcomeEmails endpoint, and the
`RecordWelcomeEmailSent` that WelcomeEmails sends crosses back. [Delivery guarantees](delivery-guarantees.md) explains why the first stays and the
others cross.

Application code outside any handler reaches an endpoint's components through its `ServiceLocator`, as the sending code
above does. The host is a convenience: an endpoint's declaration can build it without one, with
`Build(containerBuilder, environment)`, and the endpoint starts and stops itself.

## The lifecycle

- **Starting** runs three phases in order: the endpoint *listens* on a new address, *announces* the address, and starts
  *sending*. An announced address is therefore always one that is listening. When `StartAsync` completes the endpoint
  has started; finding the other endpoints goes on from there, and
  [readiness](readiness-and-waiting-sends.md) awaits it.
- **Stopping** runs the phases in reverse: it retracts the address, stops sending and stops listening, then lets what
  was already received finish, for up to 30 seconds. Disposing an endpoint stops it first. A stopped endpoint never
  starts again.
- **Letting go at once.** Cancelling the token given to `StopAsync` ends that wait, as for a process handing the
  endpoint over to its next one: the inbox handles nothing more, and the handlers still running are cancelled through
  the token they were handed, and roll back. What wasn't handled is neither lost nor marked failed: the endpoint's next
  start handles it, in its order.
- **One process at a time.** An exactly-once endpoint runs in one process at a time: starting it in a second process
  fails with `EndpointAlreadyRunningInAnotherProcessException`. The lock that ensures it dies with its process, so after
  a crash the endpoint can start again at once.
