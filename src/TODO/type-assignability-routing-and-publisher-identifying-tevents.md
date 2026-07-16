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
- `src/Compze.Teventive/Taggregates/_docs/` — `definition.md`, `introduction.md`, `taggregate-inheritance.md`,
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
- **D3 — reusable tomponents/tentities: base classes and specs DONE (2026-07-12). Re-scoped the same day:
  owned components/entities are correct as-is, WITHOUT wrapping.** Wrapping adds discriminating information
  only when the same tevent type has more than one possible publisher; an owned component's/entity's tevents
  slot into exactly one taggregate's tevent hierarchy, so the inner type alone is already the complete
  publisher identity, statically. Reusable cross-taggregate tomponents/tentities — whose tevents are adopted
  into each owner's hierarchy via owner-declared wrapper types — are implemented as `SharedTomponent`/
  `SharedTentity`/`SharedTentityCollection`/`SharedTomponentSlot`; design record and remaining sub-items in
  the Deferred section. As designed, routing/store/wire needed ZERO changes: an adopting wrapper tevent is
  just another wrapper type when it arrives.
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

- The wrapper interface is `IPublisherIdentifyingTevent<out TTevent>` alone (simplified 2026-07-13), plus the
  taggregate specialization `ITaggregateIdentifyingTevent<out T> : IPublisherIdentifyingTevent<T>`
  (`src/Compze.Teventive/Taggregates/Tevents/Public/ITaggregateIdentifyingTevent.cs`). The wrapper carries
  only publisher identity, never delivery-guarantee markers: a tevent's guarantee lives on the tevent, and
  every consumer that needs it reads it from the inner by covariance
  (`IPublisherIdentifyingTevent<IExactlyOnceTevent>`). The earlier
  `IRemotablePublisherIdentifyingTevent<...>`/`IExactlyOncePublisherIdentifyingTevent<...>` tiers — which
  re-declared the inner's guarantee interfaces onto the wrapper and forwarded its `Id`, and whose
  `where T : IExactlyOnceTevent` constraint made a remotable-but-not-exactly-once wrapper inexpressible — are
  removed; the dedup `Id` is carried as transport-envelope data, extracted once at the outbox entry (where
  the tevent-vs-tommand type is statically known), so the same delivery/storage path serves both.
- `TessageTypeInspector` enforces that any type implementing `IPublisherIdentifyingTevent<>` is generic with
  a covariant (`out`) type parameter — precisely what keeps assignability routing sound
  (`src/Compze.Abstractions/Tessaging/Validation/TessageTypeInspector.cs:99-116`). It also restricts tevent
  subscription to interfaces (`:25-36`). Both rules are unit tested
  (`test/Compze.Tests.Unit/Tessaging/TessageTypeInspectorTests.cs`).
- `TypeId` coverage for the closed generic wrapper types is validated (2026-07-12): the translated
  advertisement is asserted resolvable at endpoint start (`AssertMappingsExistFor`), and both the store's
  save path and the outbox fail loudly in `TypeMapper.GetId` for an unmapped concrete wrapper type.

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

### 4. Owned teventive components and entities — correct as-is, no wrapping (D3, re-scoped)

- `TeventiveComponent` has no wrapper type parameters
  (`src/Compze.Teventive/Taggregates/BaseClasses/TeventiveComponent.cs:6-18`); `Tentity` likewise. A
  component's `ApplyTevent` dispatches the raw inner tevent (`TeventiveComponent.cs:32`) and `Publish`
  forwards raw to the parent (`:48`), so the tevent is wrapped once, at the root, in the taggregate's own
  wrapper. That is the CORRECT design, not a gap: an owned component's tevent types belong to exactly one
  taggregate's tevent hierarchy, so the inner type alone already identifies the publisher completely —
  a wrapper level would restate statically known information and cost more generic parameters (per D3 as
  re-scoped 2026-07-12).
- Reusable (cross-taggregate) tomponents/tentities are the actual D3 work — DONE (2026-07-12):
  `SharedTomponent`, `SharedTentity`, `SharedTentityCollection`, and `SharedTomponentSlot` in
  `Compze.Teventive.Taggregates.BaseClasses`, specified end to end (wrapping, slot discrimination,
  route-back, per-tentity routing, replay, subscription grains) in
  `test/Compze.Tests.Unit/CQRS/Taggregates/SharedTomponents/`. The user-facing documentation
  (`src/Compze.Teventive/Taggregates/_docs/reusable-components.md`, still "More soon. 2024-12-14") remains
  to be rewritten to the as-built design.

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
  `ITaggregateIdentifyingTevent<>` (Compze.Teventive), and the wrapper interface
  `IPublisherIdentifyingTevent<>` (Compze.Abstractions) — the interface surfaces in `$type` references when
  wrapped histories travel inside serialized resources.
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

### 7. The remote bus — DONE except the D6 delivery guarantees (2026-07-12)

- The wire carries the fully wrapped tevent, identified by the closed wrapper type's `TypeId` (invariant 6):
  the distributed publisher hands the outbox the wrapper, the outbox stores and transmits it (the wrapper IS
  the `IExactlyOnceTevent` it stores - its `Id` is the wrapped tevent's, so exactly-once deduplication is
  unchanged), and the receiving inbox deserializes the wrapper and routes it by its type.
- Endpoints advertise tevent subscriptions in their translated wrapper form
  (`TessageHandlerRegistry.HandledRemoteTessageTypeIds`); the sender matches wrapped tevents against the
  advertised wrapper types by assignability. `AssertMappingsExistFor` on the translated advertisement
  validates `TypeId` coverage for the wrapper types at endpoint start; the publish side fails loudly in
  `TypeMapper.GetId` for an unmapped concrete wrapper.
- Publisher-conscious subscription crosses endpoints: specified end to end in
  `Publisher_conscious_subscription_tests` (a remote endpoint subscribing to
  `IMyTaggregateTevent<IMyTaggregateTevent>` receives the wrapped tevent the taggregate published).
- `IOutbox.PublishTransactionally`/`ITessagingRouter.SubscriberConnectionsFor` take
  `IPublisherIdentifyingTevent<IExactlyOnceTevent>` (covariance makes the wrapped tevent statically
  exactly-once), making an unwrapped hand-off - which no wrapper-typed route would match - a compile error
  instead of a silent routing no-op.
- The exactly-once-only tevent routing gate is REMOVED (2026-07-14): `TessagingRouter.RegisterRoutes`
  registers a route for every advertised remotable tevent subscription
  (`Is<IPublisherIdentifyingTevent<IRemotableTevent>>`), and the best-effort delivery path is built — see the
  D6 work items below. Tommand routes remain `IExactlyOnceTommand`-only (the best-effort tier is a tevent
  concept; the synchronous ask lives in Typermedia).

### 8. Test coverage — DONE for everything in scope (2026-07-12)

- The full wrapper-routing matrix on a bare `IMutableTeventDispatcher`
  (`MutableTeventDispatcher_WrappedTeventsTests.cs`); publisher identification through a real inheriting
  taggregate hierarchy including publisher-conscious and publisher-indifferent subscribers
  (`Given_a_cat_taggregate_inheriting_from_an_animal_taggregate`); subscription-level transparency and
  publisher-conscious subscription at the in-process bus
  (`Given_a_container_composed_with_InProcessTessaging`); the wrapped store round-trip including migrations
  (the store and migration suites); and publisher-conscious subscription across endpoints
  (`Publisher_conscious_subscription_tests`), with exactly-once dedup of wrapped tevents covered by the
  existing guarantee tests.

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

### Remote transport — ALL DONE (2026-07-12)

- [x] Carry the fully wrapped tevent on the wire, identified by the closed generic wrapper type's `TypeId`.
- [x] Advertise the translated wrapper subscription types; sender-side routing matches wrapped tevents
      against them by assignability. (An endpoint is one subscriber however many of its advertised
      subscriptions match - `SubscriberConnectionsFor` dedups connections.)
- [x] Inbox: dispatches received tevents by outer type (the wrapper arrives from the wire and passes
      through the `PublisherIdentifyingTevent.Wrapped` normalization); exactly-once dedup verified through
      the wrapper's `Id` (the wrapped tevent's `Id`) by the existing exactly-once guarantee tests.
- [x] `TypeId` coverage for wrapper types is validated: `AssertMappingsExistFor` on the translated
      advertisement at endpoint start, and `TypeMapper.GetId` failing loudly on the publish side.

### Remote delivery guarantees (D6) — DEFERRED (2026-07-12, after the wire increment)

Deferred by decision once the wrapped-currency work proper was complete: the exactly-once path carries
wrapped tevents end to end; widening WHICH tevents can travel remotely (and under which guarantees) is a
separate feature.

**The design for this feature is settled and lives in its own document:**
[`src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`](../Compze.Tessaging/dev_docs/tevent-delivery-model.md) —
the delivery ladder (participation / exactly-once / best-effort / observation), `IUnitOfWorkTeventPublisher`,
subscription semantics with the binary observation opt-down, the observation contract, ordering, and the
wiring rules. Designed 2026-07-13; that document is the
single home — this file keeps only the work items. Resolved along the way: the
remotable-but-not-exactly-once tier needs no new marker (`IRemotableTevent` IS the tier — a mechanism name in
a tessage type would re-entangle the axes); the wrapper-interface-constraint blocker and the
guarantee-preserving auto-wrap item both dissolved with the 2026-07-13 wrapper simplification (the wrapper
carries no guarantee interfaces, so there is nothing to mis-constrain and no wrapper class to select);
the standing "should subscribers choose a lighter guarantee?" question (`_TessageTypes..Interfaces.cs:78-79`)
is answered by the binary opt-down; and `IAtMostOnceTessage` has its reason to exist (best-effort send + `Id` +
receiver dedup — the UI double-click case), answering the `//Todo` at `_TessageTypes..Interfaces.cs:66`.
The related outbox-ordering bug (recovery reloaded the backlog in retry-metadata order instead of send order)
is FIXED (2026-07-13): `GetUndeliveredTessagesForEndpoint` orders by the outbox tessage table's monotonic
`GeneratedId` in every SQL backend.

- [x] Investigate first — RESOLVED (2026-07-14): typermedia remote routing never touches the Tessaging
      advertisement. It has its own discovery query (`TypermediaEndpointInformationQuery` →
      `HandledTypermediaTypes`, from `TypermediaHandlerRegistry.HandledRemoteTypermediaTypeIds`) and its own
      router, which routes `IAtMostOnceTypermediaTommand` and `IRemotableTuery` and skips exactly-once types.
      Two disjoint channels, so removing the Tessaging router's gate cannot break the typermedia paths — and
      only the TEVENT branch of `TessagingRouter.RegisterRoutes` widens (tommands stay exactly-once; the
      best-effort tier is a tevent concept).
- [x] Remove the exactly-once-only routing gate — DONE (2026-07-14): `TessagingRouter.RegisterRoutes` builds
      a route for every advertised remotable tevent subscription
      (`Is<IPublisherIdentifyingTevent<IRemotableTevent>>`); `TransportTessageType`/`TessageTypeTranslator`/
      `TransportRequestKind` learned the best-effort-tevent kind. Which leg a matched tevent travels is the
      published tevent's own type's decision, never routing's.
- [x] The best-effort delivery path — DONE (2026-07-14): a remotable-but-not-exactly-once tevent is sent
      without the outbox's transactional persistence (`IBestEffortTeventDeliveryLeg`, honoring the ambient
      transaction: on commit with one present, immediately otherwise; per-connection in-memory stream with
      drop-stream-whole on delivery failure) and dispatched on arrival without the inbox's persist/dedup
      (`BestEffortTeventDirectDispatcher`: own scope, own transaction, no retry — a failed handling is
      reported through the background-exception reporter). Single-in-flight per destination, acknowledgement
      after handling, so ordering holds end to end without sequence numbers while connected. Specified end
      to end in `Best_effort_tevent_delivery_tests`.
- [x] The `ITransactionIgnoringTeventPublisher` / `RegisterTransactionIgnoringTeventHandlers` escape hatches
      (immediate, out-of-transaction) and the observation dispatch — DONE (2026-07-15). The observation
      dispatcher fires at every first registration of a tevent (local publish / inbox registration after
      dedup / best-effort arrival), in a fresh scope with the ambient transaction suppressed; a throwing
      observer is reported through the background-exception reporter, never retried. Specified in
      `Tevent_observation_tests` and the in-process container specs. The publish-side escape hatch built
      alongside it (`ITransactionIgnoringTeventPublisher`, the ordinary publisher under transaction
      suppression) was DELETED (2026-07-16): nothing ever consumed it, and its semantics were contested with
      no consumer to arbitrate — see "No publish-side escape hatch" in the delivery-model doc.
- [x] Once the router honors the full advertised set, assert loudly that every advertised
      non-infrastructure type gets a route, so a future regression fails instead of silently dropping
      subscriptions — DONE (2026-07-15), on both ends: `TessageHandlerRegistry.HandledRemoteTessageTypeIds`
      asserts the advertised set's soundness at the advertising endpoint's setup (where a violation fails
      loudest), and `TessagingRouter.RegisterRoutes` asserts route-by-route that every advertised type lands
      a route. The tommand story is settled by the same assert: Tessaging routes tommands exactly-once only
      (the best-effort tier is a tevent concept; the synchronous ask lives in Typermedia), so a remotable
      non-exactly-once tommand handler fails loud instead of advertising a dead type. Alongside it the
      setup-time wiring rule and the guarantee-free `AddDistributedTessaging` composition on the database-less
      foundation are built — see `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`; the D6 feature is
      now fully implemented.
- [x] RESOLVED (2026-07-13): should tevent subscribers be able to choose a lighter delivery guarantee than
      the tevent type's own (`_TessageTypes..Interfaces.cs:78-79`)? Yes, but only as a **binary** opt-out —
      the default is the type's declared guarantee; `RegisterTransactionIgnoringTeventHandlers()` opts fully
      out for observation. No menu of intermediate levels (an intermediate tier saves no cost — it still
      needs the inbox store to dedup). See `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`.
- [x] Guarantee-preserving auto-wrap — DISSOLVED (2026-07-13). The wrapper no longer carries
      delivery-guarantee interfaces, so `PublisherIdentifyingTevent<TTevent>` closed over the inner IS the
      correct wrapper for every guarantee tier; the outbox/router/store read the guarantee from the inner by
      covariance (`IPublisherIdentifyingTevent<IExactlyOnceTevent>`), and the wire carries that same wrapper.
      There is no most-derived wrapper class to select.

### Tests

- [x] End-to-end publisher identification through a real `Taggregate` (inheritance hierarchy), including
      publisher-conscious (wrapper-typed) and publisher-indifferent subscribers
      (`Given_a_cat_taggregate_inheriting_from_an_animal_taggregate`, rewritten 2026-07-12 — it caught the
      `GenericTypeConstructor` shared-cache bug that wrapped a dog's tevents in the cat's wrapper).
      Before/After handler routing is specified at the dispatcher level
      (`MutableTeventDispatcher_WrappedTeventsTests`).
- [x] The subscription-level-transparency invariant (4) tested at every surface: dispatcher
      (`MutableTeventDispatcher_WrappedTeventsTests`), in-process bus and store publication
      (`Given_a_container_composed_with_InProcessTessaging`), remote endpoint (inner-typed and
      publisher-conscious remote subscriptions both receiving in the endpoint suites).
- [x] Make `Given_a_cat_taggregate_inheriting_from_an_animal_taggregate` assert the publisher-identifying
      routing it exists for. DONE 2026-07-12.
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

- **D3 — reusable tomponents/tentities** (re-scoped 2026-07-12: owned components/entities are correct
  as-is — see the decision log and Status §4). Base classes (`SharedTomponent`, `SharedTentity`,
  `SharedTentityCollection`, `SharedTomponentSlot`) and executable specifications DONE (2026-07-12);
  the design below is implemented and verified except where an item is marked open.
  `IPublisherIdentifyingTevent`'s own doc comment already named this use case
  (`_TessageTypes..Interfaces.cs:84-89`).

  Settled direction (as built):
  - **The adopting wrapper tevent** — the owner declares, per member slot, a wrapper type that adopts the
    reusable tomponent's tevents into the owner's tevent hierarchy; it is BOTH an owner-hierarchy tevent and
    a publisher-identifying wrapper:
    `interface IShippingAddressTevent<out T> : IOrderTevent, IPublisherIdentifyingTevent<T> where T : IPostalAddressTevent`.
    What exits the taggregate is double-wrapped — `OrderTevent<ShippingAddressTevent<PostalAddressChanged>>`,
    an `IOrderTevent<IOrderTevent>` — so the root wrapping machinery, routing, store, and wire need NOTHING
    new: just another closed wrapper type with a `TypeId`, and that closed type IS the full publication
    path, statically. Nesting composes by recursion (a reusable tomponent inside a reusable tomponent
    declares its own slots, each adoption re-rooting into the next hierarchy up).
  - **A reusable tevent is just an `ITevent` — nothing more.** No `TaggregateId`/`TaggregateVersion`
    (taggregate-ness is exactly what adoption confers), no timestamp (no gain, plenty of potential
    confusion), and no `TessageId` — `ITevent` requires none (`Id` first appears at `IAtMostOnceTessage`),
    `IPublisherIdentifyingTevent<out TTevent>` constrains its inner to bare `ITevent`, and an identity of
    its own would conflict with the one the adopting wrapper always mints. The adopting wrapper's
    implementation class derives from the owner's tevent implementation base, so `TaggregateTevent` supplies
    the bookkeeping (`TessageId`, `TaggregateId`, version, `UtcTimeStamp`) and the store's stamp-the-inner
    hydration works unchanged — the adopting wrapper IS the inner from the publisher wrapper's perspective.
    (A reusable tevent needs no `Id` of its own: the adopting wrapper wraps an identity-free inner, and the
    dedup identity for the persisted/transmitted path comes from the owning taggregate's tevent, carried as
    transport-envelope data.)
  - **A reusable tentity's identity is domain data**: one of its tevents' own properties, orthogonal to
    `TessageId`. Optionally formalized by its tevents inheriting an Id-bearing root interface — that only
    enables compile-time-safe convenience wiring; it is not required by the pattern.
  - **The owner-side slot wraps, not the tomponent.** Slot identity ("Order's SHIPPING address") is the
    owner's knowledge — the tomponent cannot truthfully stamp an identity it doesn't have. And the
    route-back registration must live owner-side anyway, so the slot holds wrap-on-publish and
    unwrap-on-apply in one place. The tomponent publishes its own raw tevents into a connection handed to
    it at construction and carries ZERO owner-related type parameters — that is what makes it reusable in
    arbitrary teventives; the generic weight lands on the owner's slot declaration, once per member.
  - **Route-back is by adopting wrapper type** on the owner's applier dispatcher, subscribed in the
    explicitly-wrapped shape:
    `.ForWrapped<IPublisherIdentifyingTevent<TAdoptingWrapperTevent>>(ownerWrapped => … apply ownerWrapped.Tevent.Tevent)` —
    by the time an adopted tevent reaches the owner's appliers it is wrapped once more in the owner's own
    publisher wrapper, routing matches the outermost type only, and covariance carries the match through.
    (An earlier note here claimed `For<IShippingAddressTevent<…>>` would work — it does not, and fails
    LOUD: the adopting wrapper interface is itself an `IPublisherIdentifyingTevent<T>`, so the one
    translation rule's identity branch treats it as an outermost wrapper type, and the dispatcher rejects
    wrapper types in `For` subscriptions outright.) Per-slot wrapper types are load-bearing, not ceremony:
    two same-typed tomponents (shipping vs. billing `PostalAddress`) publish identical inner tevent types —
    only the wrapper type distinguishes them. Verified by spec.
  - **Routing keeps exactly ONE automatic translation/unwrap level** — the publisher wrapper, invariant 2
    unchanged; deeper grains are expressed in the subscribed type and hand-unwrapped. Verified subscription
    grains: `ForWrapped<IOrderTevent<IShippingAddressTevent<Changed>>>` (full publication path) and
    `ForWrapped<IPublisherIdentifyingTevent<IShippingAddressTevent<Changed>>>` (the slot, regardless of
    owner wrapper). A bare `For<PostalAddressChanged>` does NOT match adopted publications, by decision
    (verified by spec): fully general any-depth matching is impossible with static covariance (every
    nesting level appears in the type), so it would mean walking wrapper structures at runtime — rejected;
    subscribe at the grain the static type structure expresses.
  - **Names**: eventually `TeventiveComponent` becomes `Tomponent` (the words are used far too often for
    the long form; `Tentity` already leads the way) — align the remaining `Teventive*` type and file names
    with it as part of this work.

  Resolved during implementation (2026-07-12):
  - Reusable tentity collections: `SharedTentityCollection` IS a `SharedTomponent` occupying one slot; it
    routes each tevent to the instance whose `ISharedTentityTevent<TTentityId>.EntityId` it carries.
    Nothing stamps ids: a shared tentity's tevents state their `EntityId` explicitly at creation and
    `SharedTentity.Publish` asserts the tevent carries the publishing tentity's `Id` — no
    `IGetSetTaggregateEntityTeventEntityId` analog needed.
  - Single-inheritance mechanics: the adopting wrapper implementation derives from the owner's tevent
    implementation base and implements `IPublisherIdentifyingTevent<T>` directly — a two-line class per
    slot. The close-the-wrapper-over-the-runtime-type mechanism has one home:
    `PublisherIdentifyingTevent.WrapIn` (`TaggregateIdentifyingTevent.WrapIn` delegates to it).

  Still open:
  - `reusable-components.md` (and the taggregate `_docs` generally) still describe this as future work —
    rewrite to the as-built design.
  - A store-level round-trip spec for adopted tevents. By design the store needs nothing new (the adopting
    wrapper is the inner tevent the store stamps; the row `TypeId` is the full closed outer type), but no
    executable specification pins persistence of a double-wrapped graph yet.
  - Hosting a shared tomponent inside ANOTHER shared tomponent: the slot's owner seam is
    `ITeventiveInternals<TOwnerTevent, TOwnerTeventImplementation>` — the `TaggregateTevent` world — so v1
    hosts are taggregates and owned teventive components. Shared-in-shared nesting needs a second owner
    seam for the slot (the design composes; only the seam is missing).
  - Removable shared tentities (the `TeventiveRemovableEntity` analog).
  - `PublisherIdentifyingTevent.Wrapped` normalization passes through ANY `IPublisherIdentifyingTevent<ITevent>`;
    a bare adopting wrapper reaching the in-process bus without its taggregate wrapper would pass as "fully
    wrapped" — decide whether that is unreachable by construction or needs a guard.
  - Whether the "adopted anywhere, any owner" subscription grain deserves a covariant common root interface
    for adopting wrappers (giving subscriptions the shape `IPublisherIdentifyingTevent<IAdopted…<Changed>>`)
    or stays out of the model.

## Related but out of scope here

- Migration metadata persistence incomplete (`ITeventMigration.cs:10`).
- `//performance: Use static caching trick.` on the `TessageHandlerRegistry` routing walk (`:71`).
- Open marker-interface questions in `_TessageTypes..Interfaces.cs`: commented-out `IStrictlyLocalTevent`
  (`:51-52`) and `IFireAndForgetTommand` (`:22-23`), and whether `IAtMostOnceTessage` should exist (`:66`).
- The `src/Compze.ServiceBus*` directories on disk are untracked `bin`/`obj` residue from the
  `split-servicebus-from-tessaging` branch; on `main` the bus lives in `Compze.Tessaging`.
