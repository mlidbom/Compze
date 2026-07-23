# Changelog

All notable changes to Compze.Teventive will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

- **`PublisherTevent`, `PublisherTevent<TTevent>` and `WrapTevent` leave for `Compze.Tessaging.Abstractions`.** Every tevent is wrapped before routing, which makes the wrapper a tessaging routing concept rather than a Teventive one — and they already declared a namespace neither the assembly holding them nor the concept routing them pointed at.
- `TeventDispatcherOptions` is dissolved: a single-flag enum said less than the `IgnoreUnhandled` overloads that now say it directly, including the three- and four-type forms.
- `AllowPublicSettersAttribute` is deleted, and the unreferenced `Email`/`Password` value types with it.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- Added shared tomponents and tentities: `SharedTomponent`, `SharedTentity`, `SharedTentityCollection`, and `SharedTomponentSlot` - reusable teventive components and entities that any taggregate can own, rather than being nested in one specific taggregate's tevent hierarchy. A shared teventive's tevents are bare `ITevent`s rooted in its own hierarchy; the owner declares, per member slot, an adopting wrapper tevent (an owner tevent that is also an `IPublisherIdentifyingTevent<T>` of the shared teventive's tevents), and the slot wraps every published tevent in it and routes adopted tevents back to the shared teventive's appliers - during live publishing and history replay alike. Two same-typed members (a shipping and a billing address) are told apart by their slots' adopting wrapper types; individual tentities in a collection by the `ISharedTentityTevent<TTentityId>.EntityId` their tevents carry.
- The close-the-wrapper-type-over-the-runtime-tevent-type mechanism has one home: `PublisherIdentifyingTevent.WrapIn` - taggregates wrapping in their declared wrapper implementations and shared-tomponent slots wrapping in their adopting wrapper tevents both close their wrapper type there. `TaggregateIdentifyingTevent.WrapIn` now delegates to it.
- The dispatcher now routes exclusively by the outer (wrapper) tevent type: every tevent is wrapped in an `IPublisherIdentifyingTevent<TTevent>` before routing, a subscription to an inner tevent type is a subscription to `IPublisherIdentifyingTevent<TheInnerTeventType>`, and the wrapper is opened only at delivery to inner-typed handlers. Handlers subscribed to a type the wrapper itself satisfied (such as `ITevent`) previously received the wrapper; they now receive the inner tevent like every other inner-typed subscription.
- A tevent published without a publisher-identifying wrapper is wrapped in `PublisherIdentifyingTevent<TTevent>` closed over its runtime type. The reflection-emit `WrapperTeventImplementationGenerator` is deleted: no runtime-generated tevent type may ever be sent or persisted.
- `PublisherIdentifyingTevent<TTevent>` moved from `Compze.Teventive.Taggregates.Tevents.Public` to `Compze.Teventive.Tevents.Public` - the root wrapper is a teventive-core concept, not a taggregate one - and its type parameter is renamed `TTeventInterface` -> `TTevent` since auto-wrapping closes it over concrete tevent types. The `PublisherTypeIdentifyingTevent` static helper it replaces is deleted.
- `IMutableTeventDispatcher.Handles` now answers for the same type key `Dispatch` uses, so wrapper-typed (`ForWrapped`) subscriptions are counted; previously it keyed on the raw tevent type and under-reported them.
- `BeforeHandlers`/`AfterHandlers` subscriptions now validate their subscription type like every other subscription and match per-type: a before/after handler typed narrower than the dispatched tevent filters instead of throwing `InvalidCastException`.
- Renamed `Taggregate.WrapEvent` -> `WrapTevent` and `Taggregate.WrapperTEventImplementation` -> `WrapperTeventImplementation`; file names `ITaggregateTypeIdentifyingTevent.cs` -> `ITaggregateIdentifyingTevent.cs` and `TaggregateWrapperTevent.cs` -> `TaggregateIdentifyingTevent.cs` now match the types they declare.
- `ITaggregate` now hands wrapped tevents out of every surface: `Commit` and `TeventStream` deliver exactly the wrapped instances publishing created, and `LoadFromHistory` takes the persisted wrapped tevents and applies the stored wrapper - after a migration has rewritten history, the stored wrapper is the truth, not what the taggregate would wrap today. `ITaggregate<TTevent>.TeventStream` gains precision: `IObservable<ITaggregateIdentifyingTevent<TTevent>>`.
- Extracted the taggregate's wrapping mechanism into `TaggregateIdentifyingTevent.WrapIn(wrapperTeventImplementation, tevent)` so a tevent migration author can wrap a replacement tevent in the publisher's wrapper.
- The routing model's one translation rule has one home: `PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf` - subscribing to, filtering by, or ignoring an inner tevent type means matching every `IPublisherIdentifyingTevent<TTevent>` of it.
- Added `PublisherIdentifyingTevent.Wrapped`: the normalization a boundary uses when it receives a tevent that may or may not already be wrapped - an already-wrapped tevent passes through, anything else is auto-wrapped.
- `PublisherIdentifyingTevent<>`, `TaggregateIdentifyingTevent<>`, and `ITaggregateIdentifyingTevent<>` have type-identifier mappings, so closed wrapper types can be persisted and transmitted by `TypeId`.

## 0.3.2-alpha

- Fixed packaging: the `Taggregates\_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `Taggregates\_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.3.1-alpha

- Dispatcher configuration is now immutable and supplied at creation: `IMutableTeventDispatcher.New` takes a `TeventDispatcherConfig` (option flags plus tevent types ignored when unhandled), replacing the mutating `IgnoreUnhandled`/`IgnoreAllUnhandled` registration methods.
- Renamed `ITeventHandlerRegistrar` to `ITeventSubscriber`: the object through which a party subscribes handlers to a dispatcher's tevents.
- `ITeventSubscriber` is now `IDisposable`: disposing a subscriber removes every subscription made through it from the dispatcher. Disposing twice is harmless; registering through a disposed subscriber throws.

## 0.3.0-alpha

- Initial release of extracted project
