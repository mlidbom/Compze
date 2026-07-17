# Changelog

All notable changes to Compze.Teventive.TeventStore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- The tevent store forwards taggregate tevents through the exactly-once publisher's async door, bridging in one deliberate place: the Teventive taggregate model raises tevents synchronously — from constructors and domain methods — and the store forwards them where they are raised, inside the caller's unit of work. The alternative, an async taggregate domain model, would be a redesign of Teventive itself, not a call-site choice.
- Session affinity is transactional, never thread-bound: `TeventStoreUpdater` keeps its `SingleTransactionUsageGuard` and sheds the thread-affinity half (an async unit of work legitimately migrates across threads), while `TeventStore` and the query-model generating reader — which only ever had thread affinity, and are legitimately used by several sequential transactions in one scope — lose their guard rather than gain a constraint they never had.
- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.*` — the namespaces this package's name has promised all along.
- The store's currency is now the wrapped tevent: every tevent is persisted exactly as its taggregate published it, inside its publisher's `ITaggregateIdentifyingTevent<TTeventInterface>` wrapper. A row stores the closed wrapper type's `TypeId` and the serialized wrapper object graph, so publisher identity survives storage with zero information loss; hydration deserializes the wrapper and stamps the inner tevent's column-backed properties as before.
- The migration pipeline speaks wrapped tevents end to end: `SingleTaggregateInstanceTeventStreamMutator`, `TeventModifier` (`RefactoredTevent.NewTevent` -> `NewWrappedTevent`, internal stream `Tevents` -> `WrappedTevents`), and `CompleteTeventStoreStreamMutator` all carry `ITaggregateIdentifyingTevent<ITaggregateTevent>`; the `EndOfTaggregateHistoryTeventPlaceHolder` is wrapped like every other tevent in the stream. Migration matching still inspects the inner creation tevent.
- `SelfGeneratingQueryModel` mirrors `ITeventDispatcher`'s two dispatch forms: `ApplyTevent(TTaggregateTevent)` auto-wraps a tevent arriving without a wrapper (such as one delivered to an inner-typed bus subscription), and `ApplyTevent(IPublisherIdentifyingTevent<TTaggregateTevent>)` applies an already-wrapped tevent; `LoadFromHistory` takes the persisted wrapped tevents.
- `SingleTaggregateQueryModelGenerator` feeds its dispatcher the wrapped history; `TeventStoreApi.Tueries.GetHistory<TTevent>` returns `IEnumerable<ITaggregateIdentifyingTevent<TTevent>>`.
- `ITeventStore.StreamTaggregateIdsInCreationOrder`'s tevent-type filter is translated by the routing model's one translation rule: an inner tevent type matches every wrapping of it; a wrapper type matches as it stands.
- `TeventStoreUpdater` publishes the wrapped tevent through `IUnitOfWorkTeventPublisher` (Compze.Abstractions), the one way to publish: publisher identity survives from `Publish` in the taggregate through storage and onward publication, and the tevent's own type decides how far it travels.

## 0.3.1-alpha

- Updated to stay compatible with Compze.Teventive 0.3.1-alpha: registration goes through `ITeventSubscriber`, and the query model base classes accept an optional `TeventDispatcherConfig`.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
