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
- **D7 — the store speaks wrapped through everything, not just at the persistence edge (2026-07-12).** A
  wrapper-at-the-persistence-edge variant (read APIs and the migration API keep dealing in inner tevents,
  wrapping/unwrapping at the row boundary) was proposed and REJECTED, for two reasons. Migrations: they must
  be able to stream the whole store acting on any tevent with the FULL tevent information in hand — under
  the edge variant publisher identity would be invisible and unrewritable in the one subsystem whose purpose
  is rewriting history, and the store would silently rewrap migration output "in the stream's wrapper" —
  reconstruction-from-context, which invariant 6 bans. Public interfaces: they return the real data as it
  is; an unwrapping view is a one-line projection (`Tevents()`), not an API's return type. A half-way seam
  in production code costs more over time than the one-time test migration.
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

### 2. The in-memory tevent dispatcher — DONE (2026-07-12)

`CallMatchingHandlersInRegistrationOrderTeventDispatcher` implements the decided model: a subscription to an
inner tevent type IS a subscription to `IPublisherIdentifyingTevent<T>` (`RegisteredHandler<THandledTevent>`),
routing matches on wrapper types only, unwrapping happens only at delivery to inner-typed handlers, `Handles`
keys on the same wrapper type `Dispatch` uses (so `ForWrapped` subscriptions are counted), the
unhandled-tevent ignore configuration is translated the same way subscriptions are, and
`BeforeHandlers`/`AfterHandlers` are `RegisteredHandler` subscriptions like every other (the two
`//Urgent: fix this` sites are gone). Auto-wrapping goes through the hand-written
`PublisherIdentifyingTevent<TTevent>` closed over the tevent's runtime type; the reflection-emit
`WrapperTeventImplementationGenerator` is DELETED per D4. Exhaustively unit tested
(`MutableTeventDispatcher_WrappedTeventsTests.cs`), full suite green.

### 3. Taggregate publishing — DONE (2026-07-12)

- `Taggregate.Publish` wraps every tevent in the aggregate's declared wrapper type before dispatching, and
  the wrapped instance flows through every surface: `_unCommittedTevents`, `Commit`, and `TeventStream` all
  carry the wrapped tevent (`ITaggregate<TTevent>.TeventStream` is now
  `IObservable<ITaggregateIdentifyingTevent<TTevent>>`). `LoadFromHistory` takes the persisted wrapped
  tevents and applies the STORED wrapper — after a migration has rewritten history, the stored wrapper is
  the truth, not what the taggregate would wrap today.
- Aggregate inheritance is supported: subclasses override `WrapperTeventImplementation` so each level of the
  hierarchy stamps its own publisher identity (see
  `test/Compze.Tests.Unit/CQRS/Taggregates/InheritingTaggregate/AnimalTaggregate.cs:13,29,42`).
- The wrapping mechanism has one home: `TaggregateIdentifyingTevent.WrapIn(wrapperTeventImplementation,
  tevent)` — the same call the taggregate uses when publishing and a migration author uses when wrapping a
  replacement tevent.

### 4. Teventive components and entities — not wrapping (DEFERRED, D3)

- `TeventiveComponent` has no wrapper type parameters
  (`src/Compze.Teventive/Taggregates/BaseClasses/TeventiveComponent.cs:6-18`); `TeventiveEntity` likewise.
  A component's `ApplyTevent` dispatches the raw inner tevent (`TeventiveComponent.cs:32`) and `Publish`
  forwards raw to the parent (`:48`), so a component tevent carries at most the ROOT taggregate's identity.
- Reusable (cross-aggregate) components are explicitly unsupported —
  `src/Compze.Teventive/Taggregates/_docs/reusable-components.md:1-2,16` ("wrap one more time... More
  soon. 2024-12-14").
- Deferral is safe: when component wrapping arrives it is just another wrapper type to the routing.

### 5. The tevent store — DONE (2026-07-12, per D7)

- The store's currency is the wrapped tevent, everywhere: `ITeventStore`, `ITeventStoreReader.GetHistory`,
  `ITeventStoreSerializer`, `TeventCache`, `TaggregateHistoryValidator`, and the query model feeds all deal
  in `ITaggregateIdentifyingTevent<ITaggregateTevent>`. A row stores the closed wrapper type's `TypeId` and
  the serialized WRAPPER object graph (wrappers close over the concrete inner type, so the graph
  deserializes with no polymorphic-type metadata); hydration deserializes the wrapper and stamps the inner
  tevent's column-backed properties as before. Zero information loss, no reconstruction from context.
- The migration pipeline speaks wrapped tevents end to end: `MigrateTevent` receives the full persisted
  wrapped tevent; `ITeventModifier.Replace`/`InsertBefore` take complete wrapped replacements supplied by
  the migration author; migrator selection still inspects the inner creation tevent; the
  `EndOfTaggregateHistoryTeventPlaceHolder` is wrapped like every other tevent in the pipeline. The
  perf-optimized structure of `TeventModifier`/`SingleTaggregateInstanceTeventStreamMutator` is untouched —
  element types only.
- `StreamTaggregateIdsInCreationOrder`'s filter goes through the routing model's one translation rule, now
  in its one shared home: `PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf` (also used by the
  dispatcher's ignore-configuration translation).
- Wrapper types have `TypeId` mappings: `PublisherIdentifyingTevent<>`, `TaggregateIdentifyingTevent<>`,
  `ITaggregateIdentifyingTevent<>` (Compze.Teventive), and the wrapper interfaces
  `IPublisherIdentifyingTevent<>`/`IRemotablePublisherIdentifyingTevent<>`/`IExactlyOncePublisherIdentifyingTevent<>`
  (Compze.Abstractions) — the interfaces surface in `$type` references when wrapped histories travel inside
  serialized resources.
- The `ITeventStoreTeventPublisher` seam is flipped (2026-07-12): `TeventStoreUpdater` publishes the wrapped
  tevent, and publisher identity survives from `Publish` in the taggregate through storage and onward
  publication. The one remaining unwrap, by design until the remote-transport increment: the distributed
  publisher hands the OUTBOX the inner tevent, because the wire still carries inner tevents.

### 6. The in-process bus — DONE (2026-07-12)

- The bus implements the decided model: `ForTevent<TTevent>` keys an inner tevent type subscription under
  the translated wrapper type (via `PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf`) and
  unwraps at delivery; a subscription to a wrapper type is keyed as it stands and receives the wrapper —
  publisher-conscious subscription. Registration validates with the subscription rules
  (`AssertValidForSubscription`, interfaces only), matching the in-memory dispatcher.
- Every delivery site normalizes per invariant 1 through `PublisherIdentifyingTevent.Wrapped`:
  `InProcessTeventPublisher.Publish` wraps a tevent published without a wrapper, and the inbox wraps tevents
  received from the wire (which carries inner tevents until the remote-transport increment).
- The tevent-store publishers receive and deliver the committed tevent in its wrapper; the distributed
  publisher hands the outbox the inner tevent — the wire seam, resolved by the remote-transport increment.
- Specified at the bus surface: inner-typed and wrapper-typed subscribers receive the same tevent (invariant
  4), the wrapper subscriber gets the wrapper itself, and an unwrapped publish reaches wrapper-typed
  subscribers through the auto-created wrapper
  (`test/Compze.Tests.Integration/InProcess/Given_a_container_composed_with_InProcessTessaging.cs`).
- Standing perf note at the routing site remains: `//performance: Use static caching trick.` (the
  assignability walk rebuilds the handler list on every publish).

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

### 9. Ubiquitous-language drift from the rename — RESOLVED (2026-07-12)

The concept was renamed `IWrapperTevent` → `IPublisherTypeIdentifyingTevent` → `IPublisherIdentifyingTevent`;
every artifact now speaks the current name. The `PublisherTypeIdentifyingTevent` static helper is deleted
(auto-wrap goes through `PublisherIdentifyingTevent.WrapTevent`), `definition.md` names the real wrapper
interface, file names match the types they declare (`ITaggregateIdentifyingTevent.cs`,
`TaggregateIdentifyingTevent.cs`), `Taggregate.WrapEvent`/`WrapperTEventImplementation` became
`WrapTevent`/`WrapperTeventImplementation`, and the dispatcher specs' type vocabulary is current. (The
`For`/`ForWrapped` API family and the asserter methods mirroring it keep their names by decision.)

## Work items

### Dispatcher (Compze.Teventive) — ALL DONE (2026-07-12)

- [x] Rework registration to the route-by-outer model: translate inner-type subscriptions to
      `IPublisherIdentifyingTevent<T>`, match on wrapper types only, unwrap at delivery for inner-typed
      handlers. Replaces `RegisteredHandler`'s dual-match; `Handles` then keys on the outer type and the
      `ForWrapped` under-reporting disappears. The `For`/`ForWrapped` + `ForGenericTevent`/`ForWrappedGeneric`
      API surface stays as-is (type safety + escape hatches).
- [x] Auto-wrap through `PublisherIdentifyingTevent<TTevent>` closed over the tevent's runtime
      type; then DELETE `WrapperTeventImplementationGenerator` (a temporary hack — no runtime-generated
      tevent types may ever be sent or persisted), its inner-only comment, and the unused "for
      reference" type parameters along with it.
- [x] Route `BeforeHandlers`/`AfterHandlers` through the registered-handler mechanism (the two
      `//Urgent: fix this` sites).
- [x] Replace `//Urgent: Wrapping here seems arguable at best.` with a statement of invariant 1
      (documented on `ITeventDispatcher`'s `Dispatch` overloads).

### In-process bus (Compze.Tessaging) — ALL DONE (2026-07-12)

- [x] Wrapped publication end to end: `ITaggregate.Commit` → `TeventStoreUpdater` →
      `ITeventStoreTeventPublisher` / `InProcessTeventPublisher` / `DistributedTeventStoreTeventPublisher`
      carry the wrapped tevent. (The distributed publisher hands the outbox the inner tevent until the
      remote-transport increment puts the wrapper on the wire.)
- [x] `TessageHandlerRegistry`: translate `ForTevent<T>` inner subscriptions to root-wrapper subscriptions,
      route by outer type, unwrap at delivery; support wrapper-typed (publisher-conscious) subscription.

### Tevent store — ALL DONE (2026-07-12)

- [x] Persist, load, and republish the fully wrapped tevent, type-identified by the closed generic wrapper
      type's `TypeId` (invariant 6). Store contracts retyped to `ITaggregateIdentifyingTevent<ITaggregateTevent>`.
- [x] Migrations address the full wrapped tevent (D7): `MigrateTevent` receives it, `Replace`/`InsertBefore`
      take author-supplied wrapped replacements; migrator selection stays on the inner tevent's interface
      hierarchy.

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
- [x] Fix the misnamed wrapped-dispatch tests (turned out to be three, not two — `IAdminUser...`-named tests
      asserting `IUserPublisherIdentifyingTevent<...>`). DONE 2026-07-12.
- [x] Negative (`DoesNotDispatch`) coverage for the plain `For` subscription kind, including the sharpest
      case: an admin-publisher wrapper around a non-admin tevent does NOT reach `IAdminUserTevent`
      subscribers. DONE 2026-07-12.

### Renames / ubiquitous language — DONE (2026-07-12)

- [x] Finish the rename everywhere (see Status §9): `PublisherTypeIdentifyingTevent` static class + file
      name, `definition.md`'s `IWrapperEvent`, `ITaggregateTypeIdentifyingTevent.cs` file name, and the
      `Wrapper` type vocabulary in the dispatcher tests — every artifact speaks
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
