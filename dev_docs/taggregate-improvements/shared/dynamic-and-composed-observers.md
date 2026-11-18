# Dynamic and composed observers, and SubscribeForDynamicObservers

**Decided by Magnus, 2026-11-18** (the end review's fourth question, reopened for its names, item 71). He kept the
refusal: in an endpoint, an observer created after composition of a tevent type the endpoint didn't declare is refused.
He doubted the word "runtime": "Doesn't that go for all observers, that they are runtime? … To me, dynamic, or maybe
transient, means that they can change, come and go. Unlike something specified on the container. Runtime to me is
usually a distinction between static things in code and what happens when the code executes." Two agents without the
history were asked to name them, the second also without any of the rules, and he chose the second's names: "dynamic
observer", "composed observer" and `SubscribeForDynamicObservers<TTevent>()`. Of the question, "That sounds good to me."
Specified here before it is built.

**This supersedes the names in [observing-tevents-from-other-endpoints.md](observing-tevents-from-other-endpoints.md),
and in [observing-tevents-at-runtime.md](observing-tevents-at-runtime.md) where that one names the observers.** What
they specify of the refusal and the routing stands unchanged.

## Why the names change

- **"Runtime observer" names a moment, not the difference.** Every observer observes while the program runs, and
  composition runs then too. What sets the second kind apart is that the set of them changes while the engine runs:
  each comes when created and goes when what creating it returned is disposed, and the composition can't see them.
  **Dynamic** says that.
- **"Declared observer" doesn't single out the first kind.** Dynamic observers are declared too, through the same
  `TeventObservationRegistrar`, and so is everything else on the builders. The first kind is part of the composition,
  lives as long as the container, and its types are among those an endpoint asks its peers for. **Composed** says that.
- **`ObserveFromOtherEndpoints` observes nothing,** and shares its verb with `ObserveTevents` on the same builder, which
  does create observers. In an endpoint it is also needed for a dynamic observer of tevents that only the endpoint's own
  process publishes. **`SubscribeForDynamicObservers`** names what it does, subscribing the endpoint to the type from
  its peers, and for whom; the refusal follows from it: a dynamic observer of a type not subscribed for dynamic observers
  is refused. "Subscribe" is already Compze's word for the tevent types an endpoint asks its peers for.

## What a consumer sees

- `EndpointBuilder.ObserveFromOtherEndpoints<TTevent>()` becomes `EndpointBuilder.SubscribeForDynamicObservers<TTevent>()`:

  ```csharp
  protected override void Declare(BestEffortEndpointBuilder endpoint) => endpoint.SubscribeForDynamicObservers<IAnalyticsEvent>();
  ```

- The refusal's message names the type, says that dynamic observers in an endpoint observe only what the endpoint
  subscribed for them, the endpoint's own tevents included, says why, and shows
  `endpoint.SubscribeForDynamicObservers<TheType>()`.
- `ITeventObservation` keeps its name, still marked as a placeholder for the naming pass. Its documentation, and every
  other, speaks of dynamic and composed observers.

## The renames

| Before | After |
|---|---|
| runtime observer, observer created at runtime | dynamic observer |
| observer declared at composition, declared observer | composed observer |
| `EndpointBuilder.ObserveFromOtherEndpoints<TTevent>()` | `EndpointBuilder.SubscribeForDynamicObservers<TTevent>()` |
| `TessageHandlerRegistrations.ObserveFromOtherEndpoints(Type)` | `TessageHandlerRegistrations.SubscribeForDynamicObservers(Type)` |
| `TessageHandlerRoster.TeventTypesObservedFromOtherEndpoints` | `TessageHandlerRoster.TeventTypesSubscribedForDynamicObservers` |
| `TeventObservationDispatcher.AssertDeclaredObservedFromOtherEndpoints` | `AssertSubscribedForDynamicObservers` |
| `TeventObservationDispatcher.RuntimeObservation`, `EndRuntimeObservation` | `DynamicObservation`, `EndDynamicObservation` |
| `_runtimeObserverQueues`, `_declaredObserverQueues` | `_dynamicObserverQueues`, `_composedObserverQueues` |

The changelog's entries for the spike's observation say the same in the new names.

## Specifications

The same specifications, renamed:

- `Given_a_container_composing_a_LocalTessagingEngine`'s `RuntimeObservation` part becomes its `DynamicObservation`
  part, and its classes say "a dynamic observer" where they said "an observer created at runtime".
- `Given_an_exactly_once_endpoint_with_an_observer_created_at_runtime` becomes
  `Given_an_exactly_once_endpoint_with_a_dynamic_observer`. Its refusals are of "a tevent type the endpoint did not
  subscribe for dynamic observers", and the message shows `SubscribeForDynamicObservers`.
- In the unit-of-work observation specifications, the observer "declared at composition" becomes a composed observer, and
  the one "created at runtime" a dynamic observer.
- `Given_an_endpoint_observing_best_effort_tevents_from_other_endpoints_and_an_endpoint_publishing_them` keeps its name,
  which is true; its classes say "a dynamic observer".
