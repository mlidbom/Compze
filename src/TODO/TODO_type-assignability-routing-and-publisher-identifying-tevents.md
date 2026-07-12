# TODO: Finish the type-assignability routing model and `IPublisherIdentifyingTevent` support

Status survey performed 2026-07-12 (main branch); design decisions recorded the same day. This documents the
decided target model, where each layer stands, and the work required for the model to be fully supported
across all of Compze.

## The target model — decided 2026-07-12

Core routing rule (unchanged): a subscriber to tevent type `T` receives every published tevent whose runtime
type is assignable to `T`, including through generic covariance.

Publisher identification — the decided invariants:

1. **Every tevent is wrapped before routing.** A tevent published unwrapped is immediately wrapped in
   `PublisherIdentifyingTevent<TTeventInterface>` — the hand-written implementation of the root wrapping
   interface — closed over the tevent's runtime type. There is no wrapped-vs-unwrapped special-casing
   anywhere in routing.
2. **Routing operates exclusively on the outer (wrapper) type.** A subscription to an inner tevent type `T`
   is instantly translated into a subscription to `IPublisherIdentifyingTevent<T>`; covariance then makes
   the wrapper of every tevent assignable to `T` match that subscription.
3. **Unwrapping happens only at delivery**, for subscribers whose subscribed type is an inner tevent type.
   Subscribers that name a wrapper type receive the wrapper.
4. **Subscription level is transparent.** Subscribing to `ITevent` and subscribing to
   `IPublisherIdentifyingTevent<ITevent>` receive exactly the same tevents — the latter including the
   wrapper, so it can be taken into account when needed. This holds at EVERY subscription surface: the
   in-memory dispatcher, the in-process bus, and remote endpoints.
5. **Publisher-conscious subscription is the point of the whole thing.** Subscribing to
   `IManagerTevent<IEmployeeTevent>` receives only the employee tevents published by a `Manager`, not every
   `IEmployeeTevent` from any publisher.
6. **The fully wrapped type travels everywhere, with zero information loss.** The whole wrapped tevent,
   including all its exact typing, is persisted and transmitted everywhere. The TypeIdentifiers project
   (`TypeId`) already solves identity for complex nested generic types — the store and the wire identify
   tevents by the `TypeId` of the closed generic wrapper type. No inner-only transport, no reconstruction of
   wrapper types from context, and no runtime-generated types ever sent or persisted.

Canonical statements of the model:

- `src/Compze.Abstractions/Tessaging/Public/_TessageTypes..Interfaces.cs:84-95` (the interface's doc comment)
- `src/Compze.Teventive/Taggregates/_docs/` — `definition.md`, `introduction.md`, `aggregate-inheritance.md`,
  `reusable-components.md`
- Executable specification of the dispatcher-level routing semantics:
  `test/Compze.Tests.Unit/CQRS/TeventHandling/MutableTeventDispatcher_WrappedTeventsTests.cs`

### Decision log (2026-07-12)

- **D1 — publisher identity crosses every boundary.** Wrapping is universal, not an in-aggregate feature.
  Invariants 1-5 above.
- **D2 — full-fidelity persistence and transport via the fully wrapped type.** Invariant 6. The older idea
  recorded at `WrapperTeventImplementationGenerator.cs:41-43` (transmit inner-only so endpoints need no
  wrapper type information) is REJECTED: it adds complexity to dodge a problem `TypeId` already solves
  completely. Pre-`TypeId` thinking; going by the fully wrapped types everywhere is much simpler.
- **D3 — component/entity publisher identity: DEFERRED.** Does not change what the other layers need;
  routing treats a component wrapper as just another wrapper type when it arrives.
- **D4 — two wrapping mechanisms, both compile-time types.** Taggregates wrap in their declared wrapper
  implementation types; a tevent published without a wrapper is auto-wrapped in
  `PublisherIdentifyingTevent<TTeventInterface>` closed over its runtime type. Special-casing wrapped vs.
  unwrapped throughout routing was considered and rejected. The reflection-emit
  `WrapperTeventImplementationGenerator` is NOT one of the two: it is a temporary hack to keep existing
  aggregates working until migrated to the new model, and it goes — no magical runtime-generated tevent
  types may ever be sent or persisted. This also resolves the `//Urgent: Wrapping here seems arguable at
  best.` question: wrapping at dispatch is correct by decision (the comment should be replaced by a
  statement of invariant 1).
- **D5 — route exclusively by the outer type.** Inner-type subscriptions are translated per invariant 2;
  tevents route everywhere including their wrapper and are unwrapped only at final delivery.
- **D6 — any tevent should be publishable remotely; the interfaces it implements control its delivery
  guarantees. IN SCOPE — part of this work.** Today only `IExactlyOnceTevent`/`IExactlyOnceTommand` can
  cross endpoints at all — that gate is the deficiency to remove. On the advertise-vs-route mismatch (see
  the Remote delivery guarantees work items): aligning it by narrowing advertisement to the routed set was
  proposed and REJECTED — that harmonizes on the wrong side. The advertised set (every registered remotable
  handler type) is the correct side; the mismatch is resolved by the D6 work itself, when the router honors
  everything advertised.
- **The subscription API keeps its split — it is type safety, not accident.** `For`/`ForWrapped` only
  compile when the subscribed type is statically known to belong to the dispatcher's tevent hierarchy
  (`THandledTevent : TTevent` / `TWrapperTevent : IPublisherIdentifyingTevent<TTevent>`);
  `ForGenericTevent`/`ForWrappedGeneric` are the escape hatches for subscriptions the compiler cannot
  statically verify (e.g. cross-hierarchy interfaces like `ITaggregateCreatedTevent`). The D1 examples'
  `For(...)` was conceptual shorthand, not an instruction to merge these
  (`src/Compze.Teventive/ITeventSubscriber.cs:12-24`).

## Status by layer

### 1. Message-type hierarchy and validation — DONE, with gaps

- The full wrapper hierarchy exists: `IPublisherIdentifyingTevent<out TTevent>`,
  `IRemotablePublisherIdentifyingTevent<...>`, `IExactlyOncePublisherIdentifyingTevent<...>` (whose
  deduplication `Id` defaults to the inner tevent's `Id` — `_TessageTypes..Interfaces.cs:105`), and the
  taggregate specialization `ITaggregateIdentifyingTevent<out T>`
  (`src/Compze.Teventive/Taggregates/Tevents/Public/ITaggregateTypeIdentifyingTevent.cs:6-9`).
- `TessageTypeInspector` enforces that any type implementing `IPublisherIdentifyingTevent<>` is generic with
  a covariant (`out`) type parameter — precisely what keeps assignability routing sound
  (`src/Compze.Abstractions/Tessaging/Validation/TessageTypeInspector.cs:99-116`). It also restricts tevent
  subscription to interfaces (`:25-36`). Both rules are unit tested
  (`test/Compze.Tests.Unit/Tessaging/TessageTypeInspectorTests.cs`).
- Gap: nothing yet validates `TypeId` coverage for the closed generic wrapper types that invariant 6 sends
  through the store and the wire.

### 2. The in-memory tevent dispatcher — closest to the target; needs the route-by-outer rework

`CallMatchingHandlersInRegistrationOrderTeventDispatcher` already delivers the observable behavior of
invariants 1, 3, 4 and 5 at dispatcher level, exhaustively unit tested
(`MutableTeventDispatcher_WrappedTeventsTests.cs`). But its mechanism differs from the decided model:

- Instead of translating inner subscriptions (invariant 2), `RegisteredHandler<THandledTevent>` dual-matches:
  raw assignability OR `IPublisherIdentifyingTevent<THandledTevent>` + unwrap
  (`...TeventDispatcher.RegisteredHandler.cs:22-34`). Under route-by-outer this collapses to a single
  wrapper-type match with unwrap-at-delivery for inner-typed handlers.
- `Handles(TTevent)` keys off the RAW type (`...TeventDispatcher.cs:52`) while `Dispatch` keys off the
  WRAPPED type, so `Handles` under-reports subscribers registered via `ForWrapped`. Keying everything on the
  outer type resolves this by construction.
- `BeforeHandlers`/`AfterHandlers` hard-cast-and-unwrap instead of going through the `RegisteredHandler`
  classes — `...TeventDispatcher.TeventSubscriber.cs:47` and `:58`, both flagged `//Urgent: fix this`.
- `Dispatch(TTevent)` force-wraps via `PublisherTypeIdentifyingTevent.WrapTevent`, which goes through the
  reflection-emit generator (`...TeventDispatcher.cs:40-41`). Wrapping there is correct by D4; the mechanism
  must change to `PublisherIdentifyingTevent<TTeventInterface>` and the `//Urgent` comment becomes a
  statement of invariant 1.

### 3. Taggregate publishing — DONE for aggregates and inheritance

- `Taggregate.Publish` and `Taggregate.ApplyTevent` wrap every tevent in the aggregate's declared wrapper
  type before dispatching (`src/Compze.Teventive/Taggregates/BaseClasses/Taggregate..cs:25-28, 53, 105`).
- Aggregate inheritance is supported: subclasses override `WrapperTEventImplementation` so each level of the
  hierarchy stamps its own publisher identity (see
  `test/Compze.Tests.Unit/CQRS/Taggregates/InheritingTaggregate/AnimalTaggregate.cs:13,29,42`).
- BUT: `ITaggregate.Commit` hands the store the RAW inner tevents (`Taggregate..cs:126-130`) — publisher
  identity currently dies at the aggregate boundary. Under D1 the wrapped form must survive commit and flow
  onward.

### 4. Teventive components and entities — not wrapping (DEFERRED, D3)

- `TeventiveComponent` has no wrapper type parameters
  (`src/Compze.Teventive/Taggregates/BaseClasses/TeventiveComponent.cs:6-18`); `TeventiveEntity` likewise.
  A component's `ApplyTevent` dispatches the raw inner tevent (`TeventiveComponent.cs:32`) and `Publish`
  forwards raw to the parent (`:48`), so a component tevent carries at most the ROOT taggregate's identity.
- Reusable (cross-aggregate) components are explicitly unsupported —
  `src/Compze.Teventive/Taggregates/_docs/reusable-components.md:1-2,16` ("wrap one more time... More
  soon. 2024-12-14").
- Deferral is safe: when component wrapping arrives it is just another wrapper type to the routing.

### 5. The tevent store — must persist and publish the fully wrapped tevent

- Today the store persists, loads, migrates, and republishes ONLY the inner tevent. Contracts are typed to
  `TaggregateTevent` (`src/Compze.Teventive.TeventStore.Abstractions/Internal/ITeventStoreSerializer.cs:7-9`);
  the save path casts to `TaggregateTevent` and type-identifies by the inner tevent's runtime type
  (`src/Compze.Teventive.TeventStore/TeventStore.cs:163`); onward publication after commit is raw
  (`src/Compze.Teventive.TeventStore/TeventStoreUpdater.cs:113,130`). Wrappers never reach it.
- Target per invariant 6: the store deals in the real, fully wrapped tevent type — persisted under the
  `TypeId` of the closed generic wrapper type, loaded and republished with full wrapper typing intact. No
  reconstruction of wrapper types from the taggregate type.
- Storage type identity is already the interned-integer `TypeId` via `ITypeIdInterner` + `ITypeMap`; the
  work is pointing it at the wrapped type instead of the inner type.
- Tevent migrations operate entirely on inner tevents and select migrators by interface assignability
  (`.../Refactoring/Migrations/SingleTaggregateInstanceTeventStreamMutator.cs:35`). How migrations interact
  with stored wrapper typing must be settled as part of the store work (e.g. migrate the inner tevent inside
  its wrapper).

### 6. The in-process bus — assignability done; needs wrapping + route-by-outer

- `TessageHandlerRegistry.GetTeventHandlers` routes by a pure assignability walk:
  `_teventHandlers.Where(it => it.Key.IsAssignableFrom(teventType))`
  (`src/Compze.Tessaging/Implementation/TessageHandling/Dispatching/TessageHandlerRegistry.cs:72`).
  Verified by test (`test/Compze.Tests.Integration/InProcess/Given_a_container_composed_with_InProcessTessaging.cs:44-57`).
- No wrapper awareness: the bus never wraps, never unwraps, has no subscription translation, and no
  wrapper-typed (publisher-conscious) subscription. All to be added per invariants 1-5.
- Standing perf note at the routing site: `TessageHandlerRegistry.cs:71` — `//performance: Use static
  caching trick.` (the walk rebuilds the handler list on every publish).

### 7. The remote bus — sender-side assignability done; needs wrappers on the wire

- Sender-side routing IS assignability: `TessagingRouter.SubscriberConnectionsFor` matches
  `route.TeventType.IsInstanceOfType(tevent)`
  (`.../Transport/Client/Implementation/Universal/TessagingRouter.cs:127`). Covered by the endpoint tests
  (`test/Compze.Tests.Common/.../EndpointHostTestBase.cs:133`).
- Route advertisement is the exact registered handler types as `TypeId`s, filtered to `IRemotableTessage`
  (`TessageHandlerRegistry.HandledRemoteTessageTypeIds`, `:74-86`). Under route-by-outer, endpoints
  advertise the TRANSLATED wrapper subscription types.
- The wire is exact-type + `ITypeMap` based (`TransportTessage.cs:54-58, 34-42`; `TypeMapper.GetId` throws
  for unmapped types). Per invariant 6 the wire carries the fully wrapped tevent identified by the closed
  generic wrapper type's `TypeId`; wiring that identity coverage up is part of the work.
- Gating: only `IExactlyOnceTevent`/`IExactlyOnceTommand` ever get routes
  (`TessagingRouter.RegisterRoutes`, `:96-102`), and advertisement covers all `IRemotableTessage` — the
  advertise-vs-route mismatch. Resolved by the D6 work (see the Remote delivery guarantees work items).

### 8. Test coverage — dispatcher-level only

- Covered: the full wrapper-routing matrix on a bare `IMutableTeventDispatcher`
  (`MutableTeventDispatcher_WrappedTeventsTests.cs`), in-process bus base-interface routing, remote endpoint
  base-interface routing, store round-trip of inner tevents.
- Missing: everything end-to-end. See the Tests work items.

### 9. Ubiquitous-language drift from the rename

The concept was renamed `IWrapperTevent` → `IPublisherTypeIdentifyingTevent` → `IPublisherIdentifyingTevent`,
but not every artifact speaks the current name:

- Static helper class `PublisherTypeIdentifyingTevent` and its file
  `src/Compze.Teventive/Taggregates/Tevents/Public/_TessageTypes.Classes.PublisherTypeIdentifyingTevent.cs`
  retain the intermediate name.
- `src/Compze.Teventive/Taggregates/_docs/definition.md` still uses `IWrapperEvent`.
- Interface file name `ITaggregateTypeIdentifyingTevent.cs` vs. the interface it declares,
  `ITaggregateIdentifyingTevent<T>`.
- Test vocabulary (`...WrapperTevent...` method names in `MutableTeventDispatcher_WrappedTeventsTests.cs`)
  and the `TeventDispatcherAsserter`'s `...Wrapped...` method family predate the current name.

## Work items

### Dispatcher (Compze.Teventive)

- [ ] Rework registration to the route-by-outer model: translate inner-type subscriptions to
      `IPublisherIdentifyingTevent<T>`, match on wrapper types only, unwrap at delivery for inner-typed
      handlers. Replaces `RegisteredHandler`'s dual-match; `Handles` then keys on the outer type and the
      `ForWrapped` under-reporting disappears. The `For`/`ForWrapped` + `ForGenericTevent`/`ForWrappedGeneric`
      API surface stays as-is (type safety + escape hatches).
- [ ] Auto-wrap through `PublisherIdentifyingTevent<TTeventInterface>` closed over the tevent's runtime
      type; then DELETE `WrapperTeventImplementationGenerator` (a temporary hack — no runtime-generated
      tevent types may ever be sent or persisted), its `:41-43` inner-only comment, and the unused "for
      reference" type parameters (`:10`) along with it.
- [ ] Route `BeforeHandlers`/`AfterHandlers` through the registered-handler mechanism (the two
      `//Urgent: fix this` sites, `...TeventDispatcher.TeventSubscriber.cs:47,58`).
- [ ] Replace `//Urgent: Wrapping here seems arguable at best.` (`...TeventDispatcher.cs:40`) with a
      statement of invariant 1.

### In-process bus (Compze.Tessaging)

- [ ] Wrapped publication end to end: `ITaggregate.Commit` → `TeventStoreUpdater` →
      `ITeventStoreTeventPublisher` / `InProcessTeventPublisher` / `DistributedTeventStoreTeventPublisher`
      carry the wrapped tevent.
- [ ] `TessageHandlerRegistry`: translate `ForTevent<T>` inner subscriptions to root-wrapper subscriptions,
      route by outer type, unwrap at delivery; support wrapper-typed (publisher-conscious) subscription.

### Tevent store

- [ ] Persist, load, and republish the fully wrapped tevent, type-identified by the closed generic wrapper
      type's `TypeId` (invariant 6). Retype the store contracts accordingly (today hard-typed to
      `TaggregateTevent`).
- [ ] Settle how migrations address the inner tevent inside its stored wrapper; keep migrator selection on
      the inner tevent's interface hierarchy.

### Remote transport

- [ ] Carry the fully wrapped tevent on the wire, identified by the closed generic wrapper type's `TypeId`.
- [ ] Advertise the translated wrapper subscription types; sender-side routing already matches by
      assignability (`TessagingRouter.cs:127`) and needs only the wrapper-typed routes.
- [ ] Inbox: dispatch received tevents by outer type; verify exactly-once dedup through
      `IExactlyOncePublisherIdentifyingTevent.Id` (the inner tevent's `Id`) once wrappers traverse it.
- [ ] Extend `TessageTypeInspector` (or the type-map assertions) to validate `TypeId` coverage for the
      wrapper types the store and wire now depend on.

### Remote delivery guarantees (D6)

- [ ] Investigate first: determine how typermedia remote routing consumes the advertisement today (e.g.
      whether `IAtMostOnceTypermediaTommand` handler types flow through `HandledRemoteTessageTypeIds` and
      where they route), so removing the gate does not break the typermedia paths.
- [ ] Remove the exactly-once-only routing gate: `TessagingRouter.RegisterRoutes` builds routes for every
      advertised remotable type; `TransportTessageType`/`TessageTypeTranslator` learn the delivery kinds
      beyond exactly-once.
- [ ] Implement the delivery path for remotable tevents that are not `IExactlyOnceTevent`: sent without the
      outbox's transactional persistence, dispatched on arrival without the inbox's persist/dedup — the
      guarantees each tevent's interfaces declare, no more and no less.
- [ ] Once the router honors the full advertised set, assert loudly that every advertised
      non-infrastructure type gets a route, so a future regression fails instead of silently dropping
      subscriptions.
- [ ] Standing open question, not blocking: should tevent subscribers additionally be able to choose a
      lighter delivery guarantee than the tevent type's own (`_TessageTypes..Interfaces.cs:78-79`)?

### Tests

- [ ] End-to-end publisher identification through a real `Taggregate` (inheritance hierarchy), including
      wrapper-typed subscribers and Before/After handlers.
- [ ] The subscription-level-transparency invariant (4) tested at every surface: dispatcher, in-process bus,
      store publication, remote endpoint.
- [ ] Make `Given_a_cat_taggregate_inheriting_from_an_animal_taggregate` assert the publisher-identifying
      routing it exists for
      (`test/Compze.Tests.Unit/CQRS/Taggregates/InheritingTaggregate/...cs:5` — self-flagged
      `//todo: is this supposed to actually test anything?`).
- [ ] Fix the two misnamed wrapped-dispatch tests (`MutableTeventDispatcher_WrappedTeventsTests.cs:91,114` —
      named `IAdminUserWrapperTevent_...` but asserting `IUserPublisherIdentifyingTevent<...>`).
- [ ] Negative (`DoesNotDispatch`) coverage for the plain `For`/`ForGenericTevent` subscription kinds.

### Renames / ubiquitous language

- [ ] Finish the rename everywhere (see Status §9): `PublisherTypeIdentifyingTevent` static class + file
      name, `definition.md`'s `IWrapperEvent`, `ITaggregateTypeIdentifyingTevent.cs` file name, and the
      `Wrapper` vocabulary in the dispatcher tests/asserter — every artifact speaks
      `IPublisherIdentifyingTevent`.

## Deferred

- **D3 — component/entity publisher identity.** Give `TeventiveComponent`/`TeventiveEntity` their own
  publisher-identifying wrapping; then implement reusable cross-aggregate components and finish
  `reusable-components.md`. Until then component tevents carry only the root taggregate's identity.

## Related but out of scope here

- Migration metadata persistence incomplete (`ITeventMigration.cs:10`).
- `//performance: Use static caching trick.` on the `TessageHandlerRegistry` routing walk (`:71`).
- Open marker-interface questions in `_TessageTypes..Interfaces.cs`: commented-out `IStrictlyLocalTevent`
  (`:51-52`) and `IFireAndForgetTommand` (`:22-23`), and whether `IAtMostOnceTessage` should exist (`:66`).
- The `src/Compze.ServiceBus*` directories on disk are untracked `bin`/`obj` residue from the
  `split-servicebus-from-tessaging` branch; on `main` the bus lives in `Compze.Tessaging`.
