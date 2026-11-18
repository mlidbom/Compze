# Observing tevents from other endpoints: declared on endpoints only, as ObserveFromOtherEndpoints

**Decided by Magnus, 2026-11-18** (the end review's fourth question, item 69). He questioned the name and the rule of
`AtRuntime<TTevent>()`: "It is about enabling observation of tevents from other endpoints. It is not really about
allowing runtime observation to my mind, it is about ensuring that we get these tevents from another endpoint. The setup
at composition should be named to make this clear", and "Forcing endpoints without any wire communication to declare it
first seems questionable to me." jastudio-rep-opus agreed and proposed `ReceiveFromOtherEndpoints`, declared on endpoints
only; Magnus: "maybe ObserveFromOtherEndpoints ? Otherwise go with the thinking you presented". Specified here before it
is built.

**This supersedes what [observing-tevents-at-runtime.md](observing-tevents-at-runtime.md) says about `AtRuntime` and its
rule,** in its section "What may be observed at runtime is declared at composition". The rest of that document stands.

## Why it is declared at all

An endpoint receives a tevent from another endpoint only if it asked for that type. It asks once, when it starts, for the
types its composition declares: its handlers' and its declared observers'. An observer created later, through
`ITeventObservation.ObserveTevents`, can't add to that without a round trip to every peer, during which tevents would be
missed. So a type that runtime observers are to get from other endpoints must be asked for from the start. Without a
declaration, such an observer would observe the tevents published in its own process and, silently, none from other
endpoints.

The observed type alone can't tell whether a declaration is needed: an observer of a base type also gets its subtypes,
and one may be remotable. A plain container has no other endpoints, so there nothing can be missed.

## What a consumer sees

- **An endpoint declares, where it is composed:**

  ```csharp
  endpoint.ObserveFromOtherEndpoints<IAnalyticsEvent>()
  ```

  on `EndpointBuilder`, so in an endpoint declaration's tier `Declare` override, which receives the builder whole:
  `protected override void Declare(ExactlyOnceEndpointBuilder endpoint) => endpoint.ObserveFromOtherEndpoints<IAnalyticsEvent>();`.
  The endpoint asks its peers for tevents compatible with the type, subtypes included, from its start, so a runtime
  observer of them is in effect the moment `ObserveTevents` returns, with no round trip. It registers no observer
  itself. A declared tevent that arrives while no runtime observer observes it is dropped.
- **In an endpoint, a runtime observer of an undeclared type is refused at once,** by `ObserveTevents`, with a message
  that names the type, says why, and shows the declaration: `endpoint.ObserveFromOtherEndpoints<TheType>()`.
- **In a plain container, a runtime observer may observe any tevent type,** and nothing is declared for it.
- **`AtRuntime` is gone** from `TeventObservationRegistrar` and `ITeventObservationRegistrar`: the registrar is the same
  thing at composition and at runtime, and none of its members throws for where it is used.

## How it is built

- `TessageHandlerRegistrations` knows whether it is the engine of an endpoint, as `LocalTessagingEngineBuilder` is told
  when it is made: `EndpointBuilder` makes its engine builder as an endpoint's, and a plain container's registrar makes
  one as a plain container's. The roster carries it, as `IsTheEngineOfAnEndpoint`, with the declared types.
- `EndpointBuilder.ObserveFromOtherEndpoints<TTevent>()` asserts the type is valid for subscription, and adds it to the
  declared types and to the subscribed tevent types, which the endpoint advertises to its peers, exactly as `AtRuntime`
  did. Nothing changes in routing or on the wire.
- The observation dispatcher checks each runtime observer against the declared types only in an endpoint's engine.

## Specifications

1. In a plain container, runtime observers of tevent types nothing declares observe them: each tevent, and each unit of
   work's tevents. The plain container's other runtime observation specifications declare nothing either.
2. In an endpoint, a runtime observer of a declared type observes a tevent committed in the endpoint.
3. In an endpoint, a runtime observer of an undeclared type is refused, for each tevent and for each unit of work's
   tevents, and the message names the type and shows `endpoint.ObserveFromOtherEndpoints<TheType>()`.
4. Across two endpoints, a runtime observer of a type its endpoint declared observes a best-effort tevent the other
   endpoint publishes, however soon after its creation, and stops when disposed: the earlier specification, now declared
   with `ObserveFromOtherEndpoints` in the endpoint declaration's `Declare` override.
