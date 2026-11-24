# Tessaging

Tessaging is Compze's messaging. Code that has something done, or that tells the rest of the system what happened, sends
a message, and the handlers that should receive it do, without the sender and the handlers knowing about each other.
What sets Tessaging apart is that a message's .NET type is its whole contract: Compze calls such a type-routed message a
*tessage*.

- **No routing configuration.** There are no topic names, queue names or routing keys. A tessage goes to the handlers of
  its type, and a subscriber to `IAccountTevent` receives every tevent whose type implements `IAccountTevent`, including
  kinds of tevent added long after the subscriber was written.
- **The type declares the guarantees.** Whether a tessage may leave its process, and whether its delivery is best-effort
  or exactly-once, is declared by the interfaces its type implements: neither the sender nor the receiver configures it.
- **One model, from one process to many.** The same handler declarations run in a single process's container and in
  endpoints that converse across processes. Moving a handler to another process is a change of deployment, and the
  guarantees its tessages declare move with it.

> [!NOTE]
> Compze's packages are pre-release: their APIs still change from one version to the next.

## The tessage kinds

| Kind | It says | Who handles it |
|---|---|---|
| **tevent** | Something happened. | Every handler subscribed to a compatible type: none, one or many. |
| **tommand** | Do this. | Exactly one handler. |
| **tuery** | Answer this. | Exactly one handler, which changes nothing. |

## Two ways to converse

Tessaging carries two communication styles:

- **TessageBus:** tell, and trust that it arrives. Code publishes tevents and sends tommands, and doesn't wait for the
  handlers in other endpoints to run. These pages cover the TessageBus, and what the two styles share.
- **Typermedia:** ask, and get the answer now. Tueries and typermedia tommands are executed like requests to a browsable
  API: see [Typermedia](~/Compze/Tessaging/Typermedia/_docs/introduction.md).

## A first taste

A container composes the LocalTessagingEngine with a handler for each tevent type it cares about:

[!code-csharp[](in-one-process.cs#composing_the_engine)]

Publishing a tevent runs every handler whose type it is compatible with:

[!code-csharp[](in-one-process.cs#publishing_independently)]

## The pages, in reading order

1. [Tessage types](tessage-types.md): declaring tevents, tommands and tueries, and what their types promise.
2. [In one process](in-one-process.md): the LocalTessagingEngine, handlers, and publishing tevents.
3. [Observing tevents](observing-tevents.md): watching what has happened, outside every transaction.
4. [Endpoints](endpoints.md): declaring, configuring and hosting the endpoints that converse with each other.
5. [Delivery guarantees](delivery-guarantees.md): what each guarantee means, ordering, and the consistency of an endpoint.
6. [Processes on one machine](on-one-machine.md): several processes forming one application, with no configuration.
7. [Readiness and waiting sends](readiness-and-waiting-sends.md): startup and restarts without races.

## The packages

| Package | What it brings |
|---|---|
| Compze.Tessaging.Abstractions | The tessage types and the publishing and sending interfaces: all a contracts assembly needs. |
| Compze.Tessaging | The LocalTessagingEngine, endpoints, and the named-pipe transport. |
| Compze.DependencyInjection.Microsoft | A container to compose in. Autofac, DryIoc and LightInject have packages too. |
| Compze.Hosting | `EndpointHost`, and the `InterprocessEndpointRegistry` that endpoints on one machine discover each other through. |
| Compze.Serialization.Newtonsoft | The serializer every endpoint declares. |
| Compze.Tessaging.Sqlite | The domain database of exactly-once endpoints, on SQLite. Compze.Tessaging.MicrosoftSql, .PostgreSql and .MySql serve the database servers. |
| Compze.TypeIdentifiers and Compze.TypeIdentifiers.DependencyInjection | The stable identities of the types that cross between endpoints. |
